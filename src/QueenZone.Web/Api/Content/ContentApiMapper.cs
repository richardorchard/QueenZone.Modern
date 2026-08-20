using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps repository DTOs directly to <c>/api/v1/content</c> JSON shapes.
/// Kept separate from <see cref="PublicContentMapper"/>, which serves Razor Pages.
/// </summary>
public static class ContentApiMapper
{
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
