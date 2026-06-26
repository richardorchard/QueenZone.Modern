namespace QueenZone.Data;

/// <summary>
/// Builds public image URLs from legacy PIC_FILES_T paths (e.g. "/Brian_May/3120008212.jpg").
/// The Cloudflare Worker at pictures.queenzone.org maps the first path segment, lower-cased
/// with underscores converted to hyphens, to the matching Azure Blob Storage folder.
/// </summary>
public static class PhotoImageUrl
{
    private const string PublicBaseUrl = "https://pictures.queenzone.org";

    public static string Build(string legacyPath)
    {
        var trimmed = legacyPath.TrimStart('/');
        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return $"{PublicBaseUrl}/{trimmed}";
        }

        var folder = segments[0].ToLowerInvariant().Replace('_', '-');
        var fileName = segments[^1];
        return $"{PublicBaseUrl}/{folder}/{fileName}";
    }
}
