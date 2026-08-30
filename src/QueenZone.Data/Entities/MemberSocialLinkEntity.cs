using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// One allowlisted social profile for a member. Unique on
/// (<see cref="MemberId"/>, <see cref="Channel"/>). Empty means no row.
/// <see cref="Url"/> is the canonical https URL only.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MemberSocialLinkEntity
{
    public Guid MemberId { get; set; }

    public string Channel { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public MemberAccount? Member { get; set; }
}
