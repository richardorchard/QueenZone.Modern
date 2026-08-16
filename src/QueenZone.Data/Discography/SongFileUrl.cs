namespace QueenZone.Data;

/// <summary>
/// Resolves legacy <c>Q_STAGE_T.URL</c> values (bare filenames such as
/// <c>2014417798057369.mp3</c>) to the private <c>songfiles</c> Azure Blob
/// container. Public HTML and redirects must use the member-authenticated
/// app proxy <c>/fan-performances/{id}/audio</c> — never a CDN or blob URL.
/// </summary>
public static class SongFileUrl
{
    public const string ContainerName = "songfiles";

    public static string GetBlobName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var trimmed = fileName.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return Path.GetFileName(uri.AbsolutePath);
        }

        return Path.GetFileName(trimmed.TrimStart('/'));
    }

    /// <summary>
    /// Rejects empty names and any value that looks like a path. Check the raw
    /// stored filename before <see cref="GetBlobName"/>, which would strip
    /// directory segments including <c>..</c>.
    /// </summary>
    public static bool IsSafeBlobName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var raw = fileName.Trim();
        return !raw.Contains("..", StringComparison.Ordinal)
            && !raw.Contains('/', StringComparison.Ordinal)
            && !raw.Contains('\\', StringComparison.Ordinal);
    }
}
