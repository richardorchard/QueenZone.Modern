namespace QueenZone.Data;

/// <summary>
/// Builds public cover-art URLs from legacy Q_ALBUM_T.THUMB_URL/PICTURE_URL values, which are
/// bare filenames (e.g. "queen-ii.jpg") stored in the "images/discography" Azure Blob Storage
/// folder served behind cdn.queenzone.org.
/// </summary>
public static class AlbumCoverUrl
{
    private const string PublicBaseUrl = "https://cdn.queenzone.org/images/discography";

    public static string? Build(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? null : $"{PublicBaseUrl}/{fileName.TrimStart('/')}";
}
