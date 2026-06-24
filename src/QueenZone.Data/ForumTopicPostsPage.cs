namespace QueenZone.Data;

public sealed record ForumTopicPostsPage(
    ForumTopicHeader Header,
    IReadOnlyList<ForumPostItem> Posts,
    int TotalCount,
    int Page,
    int PageSize);