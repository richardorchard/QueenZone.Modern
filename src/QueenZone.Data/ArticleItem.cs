namespace QueenZone.Data;

public sealed record ArticleItem(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? Source,
    string? CategoryName,
    bool IsPublished);