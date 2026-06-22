namespace QueenZone.Data;

public static class ArticleItemOrdering
{
    public static IReadOnlyList<ArticleItem> ByCreatedDateDescending(IEnumerable<ArticleItem> items) =>
        items
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.Id)
            .ToList();
}