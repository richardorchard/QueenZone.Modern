namespace QueenZone.Data;

/// <summary>
/// Builds public image URLs from legacy PIC_FILES_T paths (e.g. "/Brian_May/3120008212.jpg").
/// The Cloudflare Worker at pictures.queenzone.org maps the first path segment, lower-cased
/// with underscores converted to hyphens, to the matching Azure Blob Storage folder.
/// </summary>
public static class PhotoImageUrl
{
    private const string PublicBaseUrl = "https://pictures.queenzone.org";
    private const string BlobStorageBaseUrl = "https://queenzone.blob.core.windows.net";

    public static string Build(string legacyPath)
    {
        return BuildFromBase(PublicBaseUrl, legacyPath);
    }

    public static string BuildBlobStorageUrl(string legacyPath, string? blobEndpoint = null) =>
        BuildFromBase(blobEndpoint ?? BlobStorageBaseUrl, legacyPath);

    public static string ToBlobStorageUrl(string url, string? blobEndpoint = null)
    {
        var endpoint = (blobEndpoint ?? BlobStorageBaseUrl).TrimEnd('/');
        if (TryParseBlobLocation(url, out var container, out var blobName))
        {
            return $"{endpoint}/{container}/{blobName}";
        }

        return BuildBlobStorageUrl(url, endpoint);
    }

    private static string BuildFromBase(string baseUrl, string legacyPath)
    {
        var trimmed = legacyPath.TrimStart('/');
        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return $"{baseUrl.TrimEnd('/')}/{trimmed}";
        }

        var folder = segments[0].ToLowerInvariant().Replace('_', '-');
        var fileName = segments[^1];
        return $"{baseUrl.TrimEnd('/')}/{folder}/{fileName}";
    }

    /// <summary>
    /// Parses a public pictures URL into the Azure Blob container and blob name
    /// used by the Cloudflare Worker mapping.
    /// </summary>
    public static bool TryParseBlobLocation(string publicUrl, out string container, out string blobName)
    {
        container = string.Empty;
        blobName = string.Empty;
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimStart('/');
        var slashIndex = path.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= path.Length - 1)
        {
            return false;
        }

        container = path[..slashIndex];
        blobName = path[(slashIndex + 1)..];
        return container.Length > 0 && blobName.Length > 0;
    }
}
