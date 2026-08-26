using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Records that a member has deliberately Watched a forum topic for reply pushes.
/// Unique on (<see cref="MemberAccountId"/>, <see cref="TopicId"/>). Never inferred
/// from posting history.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MemberTopicWatchEntity
{
    public Guid MemberAccountId { get; set; }

    public int TopicId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public MemberAccount? MemberAccount { get; set; }
}
