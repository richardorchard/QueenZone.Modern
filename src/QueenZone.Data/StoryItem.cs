namespace QueenZone.Data;

public sealed record StoryItem(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? Source,
    string? CategoryName,
    bool IsPublished);