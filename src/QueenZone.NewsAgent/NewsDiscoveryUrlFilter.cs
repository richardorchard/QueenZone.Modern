namespace QueenZone.NewsAgent;

public static class NewsDiscoveryUrlFilter
{
    private static readonly HashSet<string> StaticFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css",
        ".js",
        ".mjs",
        ".map",
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
        ".svg",
        ".ico",
        ".avif",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
        ".mp3",
        ".mp4",
        ".webm",
        ".wav",
        ".ogg",
        ".pdf",
        ".zip"
    };

    public static bool IsLikelyArticleUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = uri.AbsolutePath;
        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension) && StaticFileExtensions.Contains(extension))
        {
            return false;
        }

        if (path.Contains("/wp-content/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/wp-includes/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.EndsWith("/xmlrpc.php", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/wlwmanifest.xml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.Equals("/wp-json", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/wp-json/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var trimmedPath = path.Trim('/');
        if (trimmedPath.Equals("news", StringComparison.OrdinalIgnoreCase)
            || trimmedPath.Equals("feed", StringComparison.OrdinalIgnoreCase)
            || trimmedPath.EndsWith("/feed", StringComparison.OrdinalIgnoreCase)
            || trimmedPath.EndsWith("/feed/rss", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
