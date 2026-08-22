namespace QueenZone.Web;

/// <summary>
/// List-card shape for <c>/api/v1/forum/categories</c>. Same public fields as the
/// website board cards on <c>/forum</c>.
/// </summary>
public sealed record ForumCategoryListItemDto(
    int Id,
    string Name,
    string? Description,
    int PostCount,
    DateTime? LastActivityAt,
    string? LatestThreadTitle,
    string DetailPath);

/// <summary>
/// Thread row for <c>/api/v1/forum/categories/{id}/topics</c>. Same public fields
/// as the website topic list on a category page.
/// </summary>
public sealed record ForumTopicListItemDto(
    int Id,
    string Title,
    DateTime LastActivityAt,
    string AuthorUsername,
    int ReplyCount,
    string? LastPostUsername,
    bool IsSticky,
    string DetailPath);
