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
/// List-card shape for <c>/api/v1/content/timeline</c>. No detail endpoint: the website
/// has no single-event page, only the one continuous timeline list.
/// </summary>
public sealed record TimelineEventDto(
    int Id,
    string Title,
    string Summary,
    DateTime EventDate,
    string FormattedDate,
    string Category,
    string CategoryLabel,
    string? SourceUrl);

/// <summary>
/// List-card shape for <c>/api/v1/content/biography</c>.
/// </summary>
public sealed record BiographyChapterListItemDto(
    int Id,
    string Title,
    string Summary,
    int DisplaySequence,
    string DetailPath);

/// <summary>
/// Detail shape for <c>/api/v1/content/biography/{id}</c>, including adjacent-chapter
/// links so the app can render prev/next navigation without a second round trip.
/// </summary>
public sealed record BiographyChapterDetailDto(
    int Id,
    string Title,
    string Summary,
    string Body,
    int DisplaySequence,
    string DetailPath,
    BiographyChapterNavDto? Previous,
    BiographyChapterNavDto? Next);

/// <summary>
/// Minimal reference to an adjacent chapter for prev/next navigation.
/// </summary>
public sealed record BiographyChapterNavDto(int Id, string Title, string DetailPath);

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

/// <summary>
/// List-card shape for <c>/api/v1/content/freddietribute</c>. No detail endpoint: the
/// website has no single-tribute page, only the paged tribute archive.
/// </summary>
public sealed record FreddieTributeDto(
    int Id,
    string Name,
    string Thought,
    string? Country,
    string DateText,
    string? TimeText);
