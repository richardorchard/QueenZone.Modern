namespace QueenZone.Data;

public sealed record SitemapContentEntry(
    int Id,
    string Title,
    DateTime PublishedAt,
    string? Slug = null);