namespace QueenZone.Web;

/// <summary>
/// Stable album card for the public discography index.
/// </summary>
public sealed record AlbumCardItem(
    int AlbumId,
    string Name,
    string Slug,
    int? ReleaseYear,
    string? ThumbnailUrl,
    string DetailPath);

/// <summary>
/// Stable album detail for the public discography album page.
/// </summary>
public sealed record AlbumDetailViewModel(
    int AlbumId,
    string Name,
    string DetailPath,
    int? ReleaseYear,
    string ArtistName,
    string? GeneralNotes,
    string? CoverUrl,
    IReadOnlyList<AlbumTrackViewModel> Songs);

/// <summary>
/// Stable track row for album detail tracklists.
/// </summary>
public sealed record AlbumTrackViewModel(
    int SongId,
    string Title,
    bool IsSingle,
    string? Lyrics,
    string? Notes);
