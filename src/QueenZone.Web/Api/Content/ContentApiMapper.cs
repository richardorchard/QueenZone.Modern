using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps repository DTOs directly to <c>/api/v1/content</c> JSON shapes.
/// Kept separate from <see cref="PublicContentMapper"/>, which serves Razor Pages.
/// </summary>
public static class ContentApiMapper
{
    public static NewsListItemDto ToNewsListItem(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.PublishedAt,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static IReadOnlyList<NewsListItemDto> ToNewsListItems(IEnumerable<NewsItem> items) =>
        items.Select(ToNewsListItem).ToList();

    public static NewsDetailDto ToNewsDetail(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            // Same sanitization / plain-text autolink path as the website news detail page.
            NewsArticleContent.FormatBody(item.Body),
            item.PublishedAt,
            item.SourceUrl,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static TimelineEventDto ToTimelineEvent(QueenHistoryEvent historyEvent) =>
        new(
            historyEvent.Id,
            historyEvent.Title,
            historyEvent.Summary,
            historyEvent.EventDate,
            historyEvent.FormattedDate,
            ToTimelineCategory(historyEvent.Category),
            ToTimelineCategoryLabel(historyEvent.Category),
            historyEvent.SourceUrl);

    public static IReadOnlyList<TimelineEventDto> ToTimelineEvents(IEnumerable<QueenHistoryEvent> events) =>
        events.Select(ToTimelineEvent).ToList();

    private static string ToTimelineCategory(QueenHistoryEventCategory category) => category switch
    {
        QueenHistoryEventCategory.Concert => "live",
        QueenHistoryEventCategory.Release or QueenHistoryEventCategory.Recording => "music",
        QueenHistoryEventCategory.Award or QueenHistoryEventCategory.Birthday or QueenHistoryEventCategory.SiteHistory => "milestone",
        _ => "other",
    };

    private static string ToTimelineCategoryLabel(QueenHistoryEventCategory category) => category switch
    {
        QueenHistoryEventCategory.Concert => "Live",
        QueenHistoryEventCategory.Release => "Release",
        QueenHistoryEventCategory.Recording => "Recording",
        QueenHistoryEventCategory.Award => "Award",
        QueenHistoryEventCategory.Birthday => "Birthday",
        QueenHistoryEventCategory.TVRadio => "TV / Radio",
        QueenHistoryEventCategory.SiteHistory => "Archive",
        _ => "Other",
    };

    public static BiographyChapterListItemDto ToBiographyChapterListItem(BiographyChapterItem chapter) =>
        new(
            chapter.Id,
            chapter.Title,
            BiographyContent.GetListSummary(chapter),
            chapter.DisplaySequence,
            BiographyRoutes.GetChapterDetailPath(chapter));

    public static IReadOnlyList<BiographyChapterListItemDto> ToBiographyChapterListItems(
        IEnumerable<BiographyChapterItem> chapters) =>
        chapters.Select(ToBiographyChapterListItem).ToList();

    public static BiographyChapterDetailDto ToBiographyChapterDetail(
        BiographyChapterItem chapter,
        BiographyChapterNav navigation) =>
        new(
            chapter.Id,
            chapter.Title,
            BiographyContent.GetListSummary(chapter),
            chapter.Body,
            chapter.DisplaySequence,
            BiographyRoutes.GetChapterDetailPath(chapter),
            ToBiographyChapterNavDto(navigation.Previous),
            ToBiographyChapterNavDto(navigation.Next));

    private static BiographyChapterNavDto? ToBiographyChapterNavDto(BiographyChapterItem? chapter) =>
        chapter is null
            ? null
            : new BiographyChapterNavDto(chapter.Id, chapter.Title, BiographyRoutes.GetChapterDetailPath(chapter));

    public static AlbumListItemDto ToAlbumListItem(AlbumSummary album) =>
        new(
            album.AlbumId,
            album.Name,
            album.ReleaseYear,
            album.ThumbnailUrl,
            DiscographyRoutes.GetAlbumPath(album));

    public static IReadOnlyList<AlbumListItemDto> ToAlbumListItems(IEnumerable<AlbumSummary> albums) =>
        albums.Select(ToAlbumListItem).ToList();

    public static AlbumDetailDto ToAlbumDetail(AlbumDetail album) =>
        new(
            album.AlbumId,
            album.Name,
            album.ReleaseYear,
            album.ArtistName,
            album.GeneralNotes,
            album.CoverUrl,
            DiscographyRoutes.GetAlbumPath(album.AlbumId, album.Slug),
            ToAlbumSongs(album.Songs));

    private static IReadOnlyList<AlbumSongDto> ToAlbumSongs(IEnumerable<AlbumSong> songs) =>
        songs
            .Select(song => new AlbumSongDto(song.SongId, song.Title, song.IsSingle, song.Lyrics, song.Notes))
            .ToList();

    public static FreddieTributeDto ToFreddieTributeDto(FreddieTribute tribute) =>
        new(
            tribute.Id,
            tribute.Name,
            tribute.Thought,
            tribute.Country,
            tribute.DateText,
            tribute.TimeText);

    public static IReadOnlyList<FreddieTributeDto> ToFreddieTributeDtos(IEnumerable<FreddieTribute> tributes) =>
        tributes.Select(ToFreddieTributeDto).ToList();

    public static PhotoCategoryListItemDto ToPhotoCategoryListItem(PhotoCategory category) =>
        new(
            category.CatId,
            category.Name,
            category.Slug,
            category.ImageCount,
            category.CoverThumbnailUrl,
            PhotoRoutes.GetCategoryPath(category.Slug));

    public static IReadOnlyList<PhotoCategoryListItemDto> ToPhotoCategoryListItems(
        IEnumerable<PhotoCategory> categories) =>
        categories.Select(ToPhotoCategoryListItem).ToList();

    public static PhotoListItemDto ToPhotoListItem(PhotoItem item, PhotoListFilter? filter = null) =>
        new(
            item.PicId,
            item.CatId,
            item.CategoryName,
            item.CategorySlug,
            item.Title,
            item.ThumbnailUrl,
            item.ThumbWidth,
            item.ThumbHeight,
            item.PictureWidth,
            item.PictureHeight,
            item.PictureDimensionsLabel,
            item.Year,
            item.DateTime,
            PhotoRoutes.GetDetailPath(item.CategorySlug, item.PicId, filter),
            PhotoRoutes.GetCategoryPath(item.CategorySlug, filter));

    public static IReadOnlyList<PhotoListItemDto> ToPhotoListItems(
        IEnumerable<PhotoItem> items,
        PhotoListFilter? filter = null) =>
        items.Select(item => ToPhotoListItem(item, filter)).ToList();

    public static PhotoDetailDto ToPhotoDetail(
        PhotoCategory category,
        PhotoDetailNavigation navigation,
        PhotoListFilter? filter = null)
    {
        var photo = navigation.Photo;
        return new PhotoDetailDto(
            photo.PicId,
            photo.CatId,
            category.Name,
            category.Slug,
            photo.Title,
            photo.ImageUrl,
            photo.ThumbnailUrl,
            photo.ThumbWidth,
            photo.ThumbHeight,
            photo.PictureWidth,
            photo.PictureHeight,
            photo.PictureDimensionsLabel,
            photo.Year,
            photo.DateTime,
            photo.SubmittedByDisplayName,
            PhotoRoutes.GetDetailPath(category.Slug, photo.PicId, filter),
            PhotoRoutes.GetCategoryPath(category.Slug, filter),
            navigation.Index,
            navigation.Count,
            ToPhotoNavDto(category.Slug, navigation.PreviousPicId, filter),
            ToPhotoNavDto(category.Slug, navigation.NextPicId, filter));
    }

    private static PhotoNavDto? ToPhotoNavDto(string slug, int? picId, PhotoListFilter? filter) =>
        picId is int id
            ? new PhotoNavDto(id, PhotoRoutes.GetDetailPath(slug, id, filter))
            : null;
}
