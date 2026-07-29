namespace QueenZone.Data;

public sealed record QueenLink(
    int Id,
    string Title,
    string Url,
    string? Comment,
    int CategoryId,
    bool IsFeatured);
