using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class HomePollEntity
{
    public Guid Id { get; set; }

    public string Question { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    /// <summary>Null means the poll is still open for votes.</summary>
    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Set the first time the poll is published. Null means it has never been live.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    public Guid CreatedByMemberId { get; set; }

    public ICollection<HomePollOptionEntity> Options { get; set; } = [];

    public ICollection<HomePollVoteEntity> Votes { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public sealed class HomePollOptionEntity
{
    public Guid Id { get; set; }

    public Guid PollId { get; set; }

    public string OptionText { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public HomePollEntity? Poll { get; set; }

    public ICollection<HomePollVoteEntity> Votes { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public sealed class HomePollVoteEntity
{
    public Guid Id { get; set; }

    public Guid PollId { get; set; }

    public Guid OptionId { get; set; }

    public Guid MemberAccountId { get; set; }

    public DateTimeOffset VotedAt { get; set; }

    public HomePollEntity? Poll { get; set; }

    public HomePollOptionEntity? Option { get; set; }
}
