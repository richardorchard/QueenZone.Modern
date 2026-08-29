namespace QueenZone.Data;

public static class ForumAuthorContentMatching
{
    public static string NormalizeDisplayName(string? displayName) =>
        (displayName ?? string.Empty).Trim().ToLowerInvariant();

    public static bool NamesEqual(string? left, string? right) =>
        string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool MatchesPost(
        Guid? memberId,
        string? displayName,
        Guid? authorMemberId,
        string? authorDisplayName)
    {
        if (memberId is Guid id && authorMemberId == id)
        {
            return true;
        }

        return authorMemberId is null && NamesEqual(authorDisplayName, displayName);
    }

    public static bool MatchesStartedThread(
        Guid? memberId,
        string? displayName,
        Guid? starterAuthorMemberId,
        string? starterAuthorDisplayName,
        string? startedByDisplayName)
    {
        if (MatchesPost(memberId, displayName, starterAuthorMemberId, starterAuthorDisplayName))
        {
            return true;
        }

        return starterAuthorMemberId is null && NamesEqual(startedByDisplayName, displayName);
    }
}
