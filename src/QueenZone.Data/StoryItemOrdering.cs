namespace QueenZone.Data;

public static class StoryItemOrdering
{
    public static IReadOnlyList<StoryItem> ByCreatedDateDescending(IEnumerable<StoryItem> items) =>
        items
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.Id)
            .ToList();
}