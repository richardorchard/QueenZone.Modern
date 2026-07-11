using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfForumPollRepository(QueenZoneDbContext dbContext, TimeProvider timeProvider) : IForumPollRepository
{
    public const int MinOptions = 2;
    public const int MaxOptions = 10;
    public const int QuestionMaxLength = 300;
    public const int OptionMaxLength = 200;

    public async Task<Guid> CreatePollAsync(
        int legacyTopicId,
        NewForumPoll poll,
        CancellationToken cancellationToken = default)
    {
        var thread = await dbContext.ModernForumThreads
            .SingleOrDefaultAsync(item => item.LegacyTopicId == legacyTopicId, cancellationToken)
            ?? throw new InvalidOperationException("Forum thread not found.");

        ValidateNewPoll(poll);

        var existing = await dbContext.ForumPolls
            .AsNoTracking()
            .AnyAsync(item => item.LegacyTopicId == legacyTopicId, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException("This thread already has a poll.");
        }

        var entity = BuildPollEntity(thread.Id, legacyTopicId, poll, timeProvider.GetUtcNow());
        dbContext.ForumPolls.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<ForumPollResults?> GetPollWithResultsAsync(
        int legacyTopicId,
        Guid? viewerMemberId,
        bool viewerIsAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var poll = await dbContext.ForumPolls
            .AsNoTracking()
            .Include(item => item.Options)
            .SingleOrDefaultAsync(item => item.LegacyTopicId == legacyTopicId, cancellationToken);
        if (poll is null)
        {
            return null;
        }

        var votes = await dbContext.ForumPollVotes
            .AsNoTracking()
            .Where(vote => vote.PollId == poll.Id)
            .Select(vote => new { vote.OptionId, vote.MemberAccountId })
            .ToListAsync(cancellationToken);

        return BuildResults(poll, votes.Select(v => (v.OptionId, v.MemberAccountId)).ToList(), viewerMemberId, viewerIsAdmin, timeProvider.GetUtcNow());
    }

    public async Task CastVoteAsync(
        Guid pollId,
        Guid memberId,
        IEnumerable<Guid> optionIds,
        CancellationToken cancellationToken = default)
    {
        var selected = optionIds.Distinct().ToArray();
        if (selected.Length == 0)
        {
            throw new ForumPollVoteException(ForumPollVoteException.InvalidOptions, "Select at least one option.");
        }

        var poll = await dbContext.ForumPolls
            .Include(item => item.Options)
            .SingleOrDefaultAsync(item => item.Id == pollId, cancellationToken)
            ?? throw new ForumPollVoteException(ForumPollVoteException.NotFound, "Poll was not found.");

        var now = timeProvider.GetUtcNow();
        if (IsClosed(poll, now))
        {
            throw new ForumPollVoteException(ForumPollVoteException.Closed, "This poll is closed.");
        }

        var alreadyVoted = await dbContext.ForumPollVotes
            .AsNoTracking()
            .AnyAsync(vote => vote.PollId == pollId && vote.MemberAccountId == memberId, cancellationToken);
        if (alreadyVoted)
        {
            throw new ForumPollVoteException(
                ForumPollVoteException.AlreadyVoted,
                "You have already voted in this poll. Votes cannot be changed.");
        }

        if (!poll.IsMultiChoice && selected.Length > 1)
        {
            throw new ForumPollVoteException(
                ForumPollVoteException.MaxChoices,
                "This poll only allows one choice.");
        }

        var maxChoices = ResolveMaxChoices(poll);
        if (selected.Length > maxChoices)
        {
            throw new ForumPollVoteException(
                ForumPollVoteException.MaxChoices,
                $"You can select at most {maxChoices} option(s).");
        }

        var validOptionIds = poll.Options.Select(option => option.Id).ToHashSet();
        if (selected.Any(id => !validOptionIds.Contains(id)))
        {
            throw new ForumPollVoteException(ForumPollVoteException.InvalidOptions, "One or more options are invalid.");
        }

        foreach (var optionId in selected)
        {
            dbContext.ForumPollVotes.Add(new ForumPollVoteEntity
            {
                Id = Guid.NewGuid(),
                PollId = pollId,
                OptionId = optionId,
                MemberAccountId = memberId,
                VotedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClosePollAsync(
        Guid pollId,
        Guid memberId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var poll = await dbContext.ForumPolls
            .SingleOrDefaultAsync(item => item.Id == pollId, cancellationToken)
            ?? throw new ForumPollVoteException(ForumPollVoteException.NotFound, "Poll was not found.");

        if (!isAdmin && poll.CreatedByMemberId != memberId)
        {
            throw new ForumPollVoteException(ForumPollVoteException.Forbidden, "Only the thread author or an admin can close this poll.");
        }

        var now = timeProvider.GetUtcNow();
        if (IsClosed(poll, now))
        {
            return;
        }

        poll.ClosedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static void ValidateNewPoll(NewForumPoll poll)
    {
        var question = poll.Question?.Trim() ?? string.Empty;
        if (question.Length is 0 or > QuestionMaxLength)
        {
            throw new ArgumentException($"Poll question is required (max {QuestionMaxLength} characters).", nameof(poll));
        }

        var options = (poll.Options ?? [])
            .Select(option => option?.Trim() ?? string.Empty)
            .Where(option => option.Length > 0)
            .ToList();

        if (options.Count is < MinOptions or > MaxOptions)
        {
            throw new ArgumentException($"Polls require between {MinOptions} and {MaxOptions} options.", nameof(poll));
        }

        if (options.Any(option => option.Length > OptionMaxLength))
        {
            throw new ArgumentException($"Each option must be at most {OptionMaxLength} characters.", nameof(poll));
        }

        if (poll.IsMultiChoice && poll.MaxChoices is < 1)
        {
            throw new ArgumentException("MaxChoices must be at least 1 for multi-choice polls.", nameof(poll));
        }
    }

    internal static ForumPollEntity BuildPollEntity(
        long threadId,
        int legacyTopicId,
        NewForumPoll poll,
        DateTimeOffset createdAt)
    {
        ValidateNewPoll(poll);
        var options = poll.Options
            .Select(option => option.Trim())
            .Where(option => option.Length > 0)
            .ToList();

        var pollId = Guid.NewGuid();
        var entity = new ForumPollEntity
        {
            Id = pollId,
            ThreadId = threadId,
            LegacyTopicId = legacyTopicId,
            Question = poll.Question.Trim(),
            IsMultiChoice = poll.IsMultiChoice,
            MaxChoices = poll.IsMultiChoice ? poll.MaxChoices : null,
            ClosesAt = poll.ClosesAt,
            CreatedByMemberId = poll.CreatedByMemberId,
            CreatedAt = createdAt,
        };

        for (var i = 0; i < options.Count; i++)
        {
            entity.Options.Add(new ForumPollOptionEntity
            {
                Id = Guid.NewGuid(),
                PollId = pollId,
                OptionText = options[i].Length <= OptionMaxLength ? options[i] : options[i][..OptionMaxLength],
                DisplayOrder = i,
            });
        }

        return entity;
    }

    internal static ForumPollResults BuildResults(
        ForumPollEntity poll,
        IReadOnlyList<(Guid OptionId, Guid MemberAccountId)> votes,
        Guid? viewerMemberId,
        bool viewerIsAdmin,
        DateTimeOffset utcNow)
    {
        var totalVotes = votes.Count;
        var distinctVoters = votes.Select(vote => vote.MemberAccountId).Distinct().Count();
        var viewerHasVoted = viewerMemberId is Guid memberId
            && votes.Any(vote => vote.MemberAccountId == memberId);
        var viewerSelected = viewerMemberId is Guid vid
            ? votes.Where(vote => vote.MemberAccountId == vid).Select(vote => vote.OptionId).ToHashSet()
            : [];

        var closed = IsClosed(poll, utcNow);
        var canVote = viewerMemberId is not null && !viewerHasVoted && !closed;
        var canClose = !closed
            && viewerMemberId is Guid closer
            && (viewerIsAdmin || poll.CreatedByMemberId == closer);

        var options = poll.Options
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.OptionText)
            .Select(option =>
            {
                var count = votes.Count(vote => vote.OptionId == option.Id);
                var percentage = totalVotes == 0 ? 0d : Math.Round(100d * count / totalVotes, 1);
                return new ForumPollOptionResult(
                    option.Id,
                    option.OptionText,
                    option.DisplayOrder,
                    count,
                    percentage,
                    viewerSelected.Contains(option.Id));
            })
            .ToList();

        return new ForumPollResults(
            poll.Id,
            poll.LegacyTopicId,
            poll.Question,
            poll.IsMultiChoice,
            poll.MaxChoices,
            poll.ClosesAt,
            poll.ClosedAt,
            poll.CreatedAt,
            poll.CreatedByMemberId,
            totalVotes,
            distinctVoters,
            viewerHasVoted,
            closed,
            canVote,
            canClose,
            options);
    }

    internal static bool IsClosed(ForumPollEntity poll, DateTimeOffset utcNow) =>
        poll.ClosedAt is not null
        || (poll.ClosesAt is DateTimeOffset closesAt && closesAt <= utcNow);

    private static int ResolveMaxChoices(ForumPollEntity poll)
    {
        if (!poll.IsMultiChoice)
        {
            return 1;
        }

        var optionCount = poll.Options.Count;
        if (poll.MaxChoices is int max && max > 0)
        {
            return Math.Min(max, optionCount);
        }

        return optionCount;
    }
}
