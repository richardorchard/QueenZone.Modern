using System.Globalization;

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

    public static string ToGalleryReference(int picId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(picId);
        return GalleryPrefix + picId.ToString(CultureInfo.InvariantCulture);
    }

    public static int? TryParseGalleryPicId(string? imageBlobKey)
    {
        if (!IsGalleryReference(imageBlobKey))
        {
            return null;
        }

        var suffix = imageBlobKey.AsSpan(GalleryPrefix.Length).Trim();
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var picId)
            && picId > 0
            ? picId
            : null;
    }

    public static AdminNewsDraft WithGalleryPick(AdminNewsDraft draft, int picId) =>
        draft with
        {
            ImageBlobKey = ToGalleryReference(picId),
            ImageGalleryPicId = picId
        };

    public static string? ArticlesBlobName(string? imageBlobKey)
    {
        if (string.IsNullOrWhiteSpace(imageBlobKey) || IsGalleryReference(imageBlobKey))
        {
            return null;
        }

        return imageBlobKey.Trim().TrimStart('/');
    }

    /// <summary>
    /// Public path of the shared default graphic used when an article has no
    /// resolvable photo. Served as a static file from QueenZone.Web.
    /// </summary>
    public const string PlaceholderPath = "/images/news-article-placeholder.svg";

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

    /// <summary>
    /// True when <see cref="ResolveImageUrl"/> produced a photo URL (not the placeholder).
    /// </summary>
    public static bool HasResolvedImage(string? imageBlobKey, int? imageGalleryPicId) =>
        ResolveImageUrl(imageBlobKey, imageGalleryPicId) is not null;

    /// <summary>
    /// Display URL for website surfaces: the resolved photo, or
    /// <see cref="PlaceholderPath"/> when the article has no image.
    /// </summary>
    public static string ResolveDisplayUrl(string? imageBlobKey, int? imageGalleryPicId) =>
        ResolveImageUrl(imageBlobKey, imageGalleryPicId) ?? PlaceholderPath;

    /// <summary>
    /// Display thumbnail URL, or <see cref="PlaceholderPath"/> when
    /// <see cref="ResolveThumbnailUrl"/> is unset.
    /// </summary>
    public static string ResolveDisplayThumbnailUrl(string? imageBlobKey, int? imageGalleryPicId) =>
        ResolveThumbnailUrl(imageBlobKey, imageGalleryPicId) ?? PlaceholderPath;
}
