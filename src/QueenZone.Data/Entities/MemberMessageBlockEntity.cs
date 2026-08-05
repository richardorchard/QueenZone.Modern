using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Records that <see cref="BlockerMemberId"/> has blocked <see cref="BlockedMemberId"/>
/// from sending them private messages.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MemberMessageBlockEntity
{
    public Guid Id { get; set; }

    public Guid BlockerMemberId { get; set; }

    public Guid BlockedMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public MemberAccount? Blocker { get; set; }

    public MemberAccount? Blocked { get; set; }
}
