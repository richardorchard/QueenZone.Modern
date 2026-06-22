namespace QueenZone.Data;

public sealed record ForumCategoryItem(
    int Id,
    string Name,
    string? Description,
    int PostCount,
    DateTime? LastActivityAt,
    string? LatestThreadTitle,
    int SortOrder);