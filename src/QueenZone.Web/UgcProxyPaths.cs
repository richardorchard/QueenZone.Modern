using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// App-proxy paths for private UGC containers (e.g. <c>/ugc/forum/{blobName}</c>).
/// Storage containers stay private; HTML never needs raw Azure blob URLs.
/// </summary>
public static class UgcProxyPaths
{
    public const string RouteTemplate = "/ugc/{area}/{*blobName}";

    public const int FullMaxLongestSide = 1200;

    public const int ThumbMaxLongestSide = 600;

    public const string WebpContentType = "image/webp";

    public static string? TryMapAreaToContainer(string? area) =>
        area?.Trim().ToLowerInvariant() switch
        {
            "forum" => BlobUploadContainers.Forum,
            "articles" => BlobUploadContainers.Articles,
            "photos" => BlobUploadContainers.Photos,
            "avatars" => BlobUploadContainers.Avatars,
            _ => null,
        };

    public static string? TryMapContainerToArea(string? container) =>
        container?.Trim() switch
        {
            BlobUploadContainers.Forum => "forum",
            BlobUploadContainers.Articles => "articles",
            BlobUploadContainers.Photos => "photos",
            BlobUploadContainers.Avatars => "avatars",
            _ => null,
        };

    public static string GetPath(string container, string blobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var area = TryMapContainerToArea(container)
            ?? throw new ArgumentException($"Unknown UGC container '{container}'.", nameof(container));

        return $"/ugc/{area}/{blobName.TrimStart('/')}";
    }

    public static string ToThumbBlobName(string blobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        const string suffix = ".webp";
        if (blobName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return blobName[..^suffix.Length] + "-thumb.webp";
        }

        return blobName + "-thumb";
    }

    /// <summary>
    /// True when <paramref name="src"/> is an app-relative UGC proxy path for a known container.
    /// </summary>
    public static bool IsProxyImageSrc(string? src)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return false;
        }

        if (!TryParseProxySrc(src, out _, out var blobName))
        {
            return false;
        }

        // Reject path tricks.
        return !blobName.Contains("..", StringComparison.Ordinal)
            && !blobName.Contains('\\', StringComparison.Ordinal);
    }

    public static bool TryParseProxySrc(
        string src,
        out string container,
        out string blobName)
    {
        container = string.Empty;
        blobName = string.Empty;

        var path = src.Trim();
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            {
                return false;
            }

            path = absolute.AbsolutePath;
        }

        if (!path.StartsWith("/ugc/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = path["/ugc/".Length..];
        var slash = remainder.IndexOf('/');
        if (slash <= 0 || slash >= remainder.Length - 1)
        {
            return false;
        }

        var area = remainder[..slash];
        var mapped = TryMapAreaToContainer(area);
        if (mapped is null)
        {
            return false;
        }

        container = mapped;
        blobName = Uri.UnescapeDataString(remainder[(slash + 1)..]);
        return !string.IsNullOrWhiteSpace(blobName);
    }
}
