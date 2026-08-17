using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Records that <see cref="FollowerMemberId"/> follows <see cref="FollowedMemberId"/>.
/// One-way: the followed member can use this as an inbound-message allow list.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MemberFollowEntity
{
    public Guid Id { get; set; }

    public Guid FollowerMemberId { get; set; }

    public Guid FollowedMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public MemberAccount? Follower { get; set; }

    public MemberAccount? Followed { get; set; }
}
