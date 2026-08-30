namespace QueenZone.Web;

/// <summary>
/// List-card shape for <c>/api/v1/content/news</c>. Deliberately decoupled from
/// <see cref="NewsArchiveItem"/> so changes to the Razor Pages view model do not
/// silently change the mobile JSON contract.
/// <c>ImageUrl</c> / <c>ThumbnailUrl</c> are resolved public URLs (or
/// <see langword="null"/> when the article has no image or the reference is a
/// gallery pick). The database stores blob keys only — never image bytes.
/// </summary>
public sealed record NewsListItemDto(
    int Id,
    string Title,
    string Excerpt,
    DateTime PublishedAt,
    string DetailPath,
    string? ImageUrl = null,
    string? ThumbnailUrl = null,
    int? TopicId = null,
    int? ReplyCount = null);

/// <summary>
/// Last-N forum reply preview for news detail. Not the opening post.
/// </summary>
public sealed record NewsDiscussionPreviewDto(
    string AuthorDisplayName,
    DateTime PostedAt,
    string Excerpt);

/// <summary>
/// Detail shape for <c>/api/v1/content/news/{id}</c>.
/// <c>Body</c> is sanitized HTML suitable for display (same allowlist as
/// <see cref="NewsArticleContent.FormatBody"/>): basic formatting, links, and UGC images.
/// Plain-text legacy bodies are HTML-encoded with line breaks and auto-linked URLs.
/// <c>ImageUrl</c> / <c>ThumbnailUrl</c> follow the same resolved-URL contract as
/// <see cref="NewsListItemDto"/>.
/// </summary>
public sealed record NewsDetailDto(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl,
    string DetailPath,
    string? ImageUrl = null,
    string? ThumbnailUrl = null,
    int? TopicId = null,
    int? DiscussionReplyCount = null,
    IReadOnlyList<NewsDiscussionPreviewDto>? DiscussionPreview = null);

/// <summary>
/// Earliest/latest published years for <c>/api/v1/content/news/years</c>. Backs the mobile
/// year-rail scrubber's tick marks (issue #886); both are <see langword="null"/> when the
/// archive has no published articles.
/// </summary>
public sealed record NewsYearRangeDto(int? MinYear, int? MaxYear);
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

/// <summary>
/// Shape for <c>/api/v1/content/live-activity</c>. No presence/heartbeat tracking exists,
/// so this deliberately carries only the honestly-computable count of forum replies posted
/// today, not a "members reading" figure.
/// </summary>
public sealed record LiveActivitySummaryDto(int NewForumRepliesToday);

/// <summary>
/// Shape for <c>/api/v1/content/quotes/random</c> and <c>/api/v1/content/quotes/{id}</c>.
/// <see cref="Context"/> is the existing <c>QUEEN_QUOTE_T.CONTEXT</c> column (nullable).
/// </summary>
public sealed record QuoteDto(int Id, string Text, string WhoSaid, string? Context);

/// <summary>
/// Category card for <c>/api/v1/content/photos/categories</c> and
/// <c>/api/v1/content/photos/categories/{slug}</c>. Cover URLs are CDN
/// (<c>cdn.queenzone.org</c>) via <see cref="QueenZone.Data.PhotoImageUrl"/>.
/// </summary>
public sealed record PhotoCategoryListItemDto(
    int CatId,
    string Name,
    string Slug,
    int ImageCount,
    string? CoverThumbnailUrl,
    string DetailPath);

/// <summary>
/// Thumbnail-grid card for <c>/api/v1/content/photos/categories/{slug}/items</c>.
/// Includes the CDN thumbnail only — full <c>ImageUrl</c> is reserved for detail
/// so clients do not load originals in a gallery grid.
/// </summary>
public sealed record PhotoListItemDto(
    int PicId,
    int CatId,
    string CategoryName,
    string CategorySlug,
    string Title,
    string ThumbnailUrl,
    int ThumbWidth,
    int ThumbHeight,
    int PictureWidth,
    int PictureHeight,
    string? PictureDimensionsLabel,
    int Year,
    DateTime DateTime,
    string DetailPath,
    string CategoryPath);

/// <summary>
/// Detail shape for <c>/api/v1/content/photos/categories/{slug}/items/{picId}</c>,
/// including prev/next neighbors (same order as the website lightbox).
/// <c>ImageUrl</c> is the CDN original from <see cref="QueenZone.Data.PhotoImageUrl"/>.
/// </summary>
public sealed record PhotoDetailDto(
    int PicId,
    int CatId,
    string CategoryName,
    string CategorySlug,
    string Title,
    string ImageUrl,
    string ThumbnailUrl,
    int ThumbWidth,
    int ThumbHeight,
    int PictureWidth,
    int PictureHeight,
    string? PictureDimensionsLabel,
    int Year,
    DateTime DateTime,
    string? SubmittedByDisplayName,
    string DetailPath,
    string CategoryPath,
    int Index,
    int Count,
    PhotoNavDto? Previous,
    PhotoNavDto? Next);

/// <summary>
/// Minimal reference to an adjacent photo for prev/next navigation.
/// </summary>
public sealed record PhotoNavDto(int PicId, string DetailPath);

/// <summary>
/// List and detail shape for <c>/api/v1/content/fan-performances</c>.
/// <c>AudioPath</c> is the member-gated stream; the listing itself is public,
/// matching <c>/fan-performances</c>. Duration is MPEG metadata when the
/// songfile is readable, otherwise the optional domain value (sample data).
/// </summary>
public sealed record FanPerformanceDto(
    int Id,
    string Title,
    string PerformedBy,
    string Description,
    DateTime DateAdded,
    int? DurationSeconds,
    string DetailPath,
    string AudioPath);
