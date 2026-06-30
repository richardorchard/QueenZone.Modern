namespace QueenZone.Data;

public sealed record AlbumSummary(
    int AlbumId,
    string Name,
    string Slug,
    int? ReleaseYear,
    string? ThumbnailUrl);
