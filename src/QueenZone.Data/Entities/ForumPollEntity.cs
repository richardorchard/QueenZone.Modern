using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class ForumPollEntity
{
    public Guid Id { get; set; }

    /// <summary>Internal modern thread row id.</summary>
    public long ThreadId { get; set; }

    /// <summary>Public topic id used in forum URLs.</summary>
    public int LegacyTopicId { get; set; }

    public string Question { get; set; } = string.Empty;

    public bool IsMultiChoice { get; set; }

    public int? MaxChoices { get; set; }

    public DateTimeOffset? ClosesAt { get; set; }

    /// <summary>Set when closed early by author/admin; null if only using <see cref="ClosesAt"/>.</summary>
    public DateTimeOffset? ClosedAt { get; set; }

    public Guid CreatedByMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ModernForumThreadEntity? Thread { get; set; }

    public ICollection<ForumPollOptionEntity> Options { get; set; } = [];

    public ICollection<ForumPollVoteEntity> Votes { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public sealed class ForumPollOptionEntity
{
    public Guid Id { get; set; }

    public Guid PollId { get; set; }

    public string OptionText { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public ForumPollEntity? Poll { get; set; }

    public ICollection<ForumPollVoteEntity> Votes { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public sealed class ForumPollVoteEntity
{
    public Guid Id { get; set; }

    public Guid PollId { get; set; }

    public Guid OptionId { get; set; }

    public Guid MemberAccountId { get; set; }

    public DateTimeOffset VotedAt { get; set; }

    public ForumPollEntity? Poll { get; set; }

    public ForumPollOptionEntity? Option { get; set; }
}
