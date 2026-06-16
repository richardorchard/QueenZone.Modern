namespace QueenZone.Data;

public static class NewsItemOrdering
{
    public static IReadOnlyList<NewsItem> ByCreatedDateDescending(IEnumerable<NewsItem> items) =>
        items
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.Id)
            .ToList();
}