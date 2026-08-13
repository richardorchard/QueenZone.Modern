namespace QueenZone.Data;

public sealed record AdminNewsArticlePage(
    IReadOnlyList<AdminNewsArticle> Items,
    int TotalCount,
    int Page,
    int PageSize);
