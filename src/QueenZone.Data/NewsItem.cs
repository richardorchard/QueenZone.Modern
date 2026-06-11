namespace QueenZone.Data;

public sealed record NewsItem(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl,
    bool IsPublished);
