namespace QueenZone.Data;

/// <summary>
/// Identity for an unlinked legacy forum author, derived from their own posts (there is no
/// separate legacy member table read here). <see cref="DisplayName"/> is taken from the most
/// recent post, since the same legacy user id can carry slightly different display names across
/// posts.
/// </summary>
public sealed record ForumArchiveAuthorSummary(
    int LegacyUserId,
    string DisplayName,
    DateTime? MemberSince,
    int PostCount);
