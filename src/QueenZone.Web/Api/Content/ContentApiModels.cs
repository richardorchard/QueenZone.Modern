namespace QueenZone.Web;

/// <summary>
/// List-card shape for <c>/api/v1/content/news</c>. Deliberately decoupled from
/// <see cref="NewsArchiveItem"/> so changes to the Razor Pages view model do not
/// silently change the mobile JSON contract.
/// </summary>
public sealed record NewsListItemDto(
    int Id,
    string Title,
    string Excerpt,
    DateTime PublishedAt,
    string DetailPath);

/// <summary>
/// Detail shape for <c>/api/v1/content/news/{id}</c>.
/// </summary>
public sealed record NewsDetailDto(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl,
    string DetailPath);

/// <summary>
/// List-card shape for <c>/api/v1/content/discography</c>.
/// </summary>
public sealed record AlbumListItemDto(
    int AlbumId,
    string Name,
    int? ReleaseYear,
    string? ThumbnailUrl,
    string DetailPath);

/// <summary>
/// Detail shape for <c>/api/v1/content/discography/{id}</c>, including the track list.
/// </summary>
public sealed record AlbumDetailDto(
    int AlbumId,
    string Name,
    int? ReleaseYear,
    string ArtistName,
    string? GeneralNotes,
    string? CoverUrl,
    string DetailPath,
    IReadOnlyList<AlbumSongDto> Songs);

/// <summary>
/// A single track within an <see cref="AlbumDetailDto"/>.
/// </summary>
public sealed record AlbumSongDto(int SongId, string Title, bool IsSingle, string? Lyrics, string? Notes);
