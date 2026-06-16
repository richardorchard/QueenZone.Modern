namespace QueenZone.Data;

public sealed record AdminNewsDraft(
    string Title,
    string? Slug,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl);