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
            item.Body,
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
}
