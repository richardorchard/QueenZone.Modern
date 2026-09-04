namespace QueenZone.Routing;

/// <summary>
/// Stable board summary for the public forum index and category headers.
/// </summary>
public sealed record ForumCategorySummary(
    int Id,
    string Name,
    string? Description,
    int PostCount,
    DateTime? LastActivityAt,
    string? LatestThreadTitle,
    string DetailPath);

/// <summary>
/// Stable thread row for public category topic lists.
/// </summary>
public sealed record ForumThreadSummary(
    int Id,
    string Title,
    DateTime LastActivityAt,
    string AuthorUsername,
    int ReplyCount,
    string? LastPostUsername,
    bool IsSticky,
    string DetailPath);

/// <summary>
/// Stable thread header for public topic pages.
/// </summary>
public sealed record ForumThreadHeader(
    int TopicId,
    string Title,
    int ForumId,
    string ForumName,
    string CategoryPath,
    string DetailPath,
    bool? HasPoll = null);
