namespace QueenZone.Data;

public sealed record AdminNewsArticle(
    int Id,
    string Title,
    string Slug,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl,
    bool IsPublished,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    string? EditorEmail);