using QueenZone.Data;

namespace QueenZone.Web;

public static class DiscographyRoutes
{
    public static string GetIndexPath() => "/discography";

    public static string GetAlbumPath(int albumId, string slug) => $"/discography/albums/{albumId}/{slug}";

    public static string GetAlbumPath(AlbumSummary album) => GetAlbumPath(album.AlbumId, album.Slug);
}
