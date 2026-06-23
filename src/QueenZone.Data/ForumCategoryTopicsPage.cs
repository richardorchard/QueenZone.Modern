namespace QueenZone.Data;

public sealed record ForumCategoryTopicsPage(
    IReadOnlyList<ForumTopicItem> Topics,
    int TotalCount,
    int Page,
    int PageSize);