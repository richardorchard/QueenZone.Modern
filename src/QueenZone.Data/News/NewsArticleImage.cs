namespace QueenZone.Data;

/// <summary>
/// News article image reference helpers. The database stores blob keys and optional
/// gallery pic ids only — never image bytes.
/// </summary>
/// <remarks>
/// <para>
/// <c>ImageBlobKey</c> is either a <c>ugc-articles</c> blob name or a
/// <c>gallery:{picId}</c> prefix. Do not assume every value is a ugc-articles name.
/// <c>ImageGalleryPicId</c> is the typed gallery pick for later stories.
/// </para>
/// <para>
/// Full-size and thumbnail share a name via the existing <c>-thumb.webp</c> /
/// <c>?size=thumb</c> convention; there is no second thumbnail column.
/// </para>
/// </remarks>
public static class NewsArticleImage
{
    public const string GalleryPrefix = "gallery:";

    public const int MaxBlobKeyLength = 512;

    public static bool IsGalleryReference(string? imageBlobKey) =>
        !string.IsNullOrWhiteSpace(imageBlobKey)
        && imageBlobKey.StartsWith(GalleryPrefix, StringComparison.OrdinalIgnoreCase);

    public static string? ArticlesBlobName(string? imageBlobKey)
    {
        if (string.IsNullOrWhiteSpace(imageBlobKey) || IsGalleryReference(imageBlobKey))
        {
            return null;
        }

        return imageBlobKey.Trim().TrimStart('/');
    }

    /// <summary>
    /// Resolved public image URL, or <see langword="null"/> when unset or when the
    /// reference is a gallery pick (resolved later via <c>PIC_FILES_T</c>).
    /// </summary>
    public static string? ResolveImageUrl(string? imageBlobKey, int? imageGalleryPicId)
    {
        if (imageGalleryPicId is > 0 || IsGalleryReference(imageBlobKey))
        {
            return null;
        }

        var blobName = ArticlesBlobName(imageBlobKey);
        return blobName is null ? null : $"/ugc/articles/{blobName}";
    }

    /// <summary>
    /// Resolved thumbnail URL using <c>?size=thumb</c>, or <see langword="null"/> when
    /// <see cref="ResolveImageUrl"/> is unset.
    /// </summary>
    public static string? ResolveThumbnailUrl(string? imageBlobKey, int? imageGalleryPicId)
    {
        var imageUrl = ResolveImageUrl(imageBlobKey, imageGalleryPicId);
        return imageUrl is null ? null : imageUrl + "?size=thumb";
    }
}
