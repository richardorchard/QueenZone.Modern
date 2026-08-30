using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfHomePollRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly QueenZoneDbContext dbContext;
    private readonly EfHomePollRepository repository;

    public EfHomePollRepositoryTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        repository = new EfHomePollRepository(dbContext, TimeProvider.System);
    }

    [Fact]
    public async Task Filtered_unique_index_allows_only_one_current_poll()
    {
        var first = await repository.CreateAsync(new AdminHomePollDraft("First?", ["A", "B"]), Guid.NewGuid());
        var second = await repository.CreateAsync(new AdminHomePollDraft("Second?", ["C", "D"]), Guid.NewGuid());
        await repository.PublishAsync(first);
        await repository.PublishAsync(second);

        var current = await repository.GetCurrentAsync(null);
        Assert.Equal(second, current!.PollId);
        Assert.Equal(1, await dbContext.HomePolls.CountAsync(poll => poll.IsCurrent));
    }

    [Fact]
    public async Task CastVote_persists_one_ballot_and_rejects_the_second()
    {
        var pollId = await repository.CreateAsync(new AdminHomePollDraft("Q?", ["Yes", "No"]), Guid.NewGuid());
        await repository.PublishAsync(pollId);
        var current = await repository.GetCurrentAsync(null);
        var member = Guid.NewGuid();

        await repository.CastVoteAsync(current!.Options[0].OptionId, member);
        var voted = await repository.GetCurrentAsync(member);
        Assert.True(voted!.ViewerHasVoted);
        Assert.Equal(1, voted.TotalVotes);
        Assert.Equal(100, voted.Options[0].Percentage);

        var second = await Assert.ThrowsAsync<ForumPollVoteException>(() =>
            repository.CastVoteAsync(current.Options[1].OptionId, member));
        Assert.Equal(ForumPollVoteException.AlreadyVoted, second.Code);
        Assert.Equal(1, (await repository.GetCurrentAsync(null))!.TotalVotes);
    }

    [Fact]
    public async Task Existing_ballot_row_is_treated_as_already_voted()
    {
        var pollId = await repository.CreateAsync(new AdminHomePollDraft("Q?", ["A", "B"]), Guid.NewGuid());
        await repository.PublishAsync(pollId);
        var current = await repository.GetCurrentAsync(null);
        var member = Guid.NewGuid();
        dbContext.HomePollVotes.Add(new QueenZone.Data.Entities.HomePollVoteEntity
        {
            Id = Guid.NewGuid(),
            PollId = pollId,
            OptionId = current!.Options[0].OptionId,
            MemberAccountId = member,
            VotedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ForumPollVoteException>(() =>
            repository.CastVoteAsync(current.Options[1].OptionId, member));
        Assert.Equal(ForumPollVoteException.AlreadyVoted, ex.Code);
        Assert.Equal(1, (await repository.GetCurrentAsync(null))!.TotalVotes);
    }

    [Fact]
    public async Task Update_after_vote_is_rejected()
    {
        var pollId = await repository.CreateAsync(new AdminHomePollDraft("Q?", ["A", "B"]), Guid.NewGuid());
        await repository.PublishAsync(pollId);
        var current = await repository.GetCurrentAsync(null);
        await repository.CastVoteAsync(current!.Options[0].OptionId, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<HomePollException>(() =>
            repository.UpdateAsync(pollId, new AdminHomePollDraft("Nope", ["X", "Y"])));
        Assert.Equal(HomePollException.HasVotes, ex.Code);
    }

    [Fact]
    public void IsUniqueConstraintViolation_detects_sqlite_and_sql_server_messages()
    {
        var sqlite = new DbUpdateException(
            "fail",
            new Exception("UNIQUE constraint failed: HomePollVotes.PollId"));
        var sqlServer = new DbUpdateException(
            "fail",
            new Exception("Cannot insert duplicate key row in object with unique index"));
        Assert.True(EfHomePollRepository.IsUniqueConstraintViolation(sqlite));
        Assert.True(EfHomePollRepository.IsUniqueConstraintViolation(sqlServer));
        Assert.False(EfHomePollRepository.IsUniqueConstraintViolation(
            new DbUpdateException("other", new Exception("timeout"))));
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
