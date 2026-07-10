namespace QueenZone.Data;

/// <summary>
/// Builds public audio URLs from legacy Q_STAGE_T.URL values, which are bare filenames
/// (e.g. "2014417798057369.mp3") stored in the "songfiles" Azure Blob Storage container,
/// served via the cdn.queenzone.org straight Cloudflare CDN proxy.
/// </summary>
public static class SongFileUrl
{
    private const string PublicBaseUrl = "https://cdn.queenzone.org/songfiles";

    public static string Build(string fileName) => $"{PublicBaseUrl}/{fileName.TrimStart('/')}";
}
