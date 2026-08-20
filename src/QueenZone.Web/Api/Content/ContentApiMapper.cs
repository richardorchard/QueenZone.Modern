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
