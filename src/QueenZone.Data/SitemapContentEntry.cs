namespace QueenZone.Data;

public sealed class SitemapContentEntry
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public string? Slug { get; init; }

    public SitemapContentEntry()
    {
    }

    public SitemapContentEntry(int id, string title, DateTime publishedAt, string? slug = null)
    {
        Id = id;
        Title = title;
        PublishedAt = publishedAt;
        Slug = slug;
    }
}