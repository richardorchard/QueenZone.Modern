using System.Globalization;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

/// <summary>
/// Shared helpers for the article-form gallery picker. Lists existing PIC rows
/// through <see cref="IAdminPhotoRepository"/> — it does not upload or copy blobs.
/// </summary>
public static class NewsArticleGalleryPicker
{
    public const int PageSize = 8;

    public const string Path = "/admin/news/gallery-picker";

    public static AdminPhotoListFilter BuildFilter(int? catId, string? search) =>
        new(CatId: catId, Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim());

    public static string FileName(AdminPhotoItem item)
    {
        var path = (item.LegacyUrl ?? string.Empty).Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(name) ? item.Title : name;
    }

    public static string BuildPath(int? catId, string? search, int page)
    {
        var parts = new List<string>();
        if (catId is int id)
        {
            parts.Add("catId=" + id.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add("q=" + Uri.EscapeDataString(search.Trim()));
        }

        if (page > 1)
        {
            parts.Add("pageNumber=" + page.ToString(CultureInfo.InvariantCulture));
        }

        return parts.Count == 0 ? Path : Path + "?" + string.Join("&", parts);
    }

    public static async Task<string?> ResolvePreviewUrlAsync(
        IAdminPhotoRepository photos,
        AdminNewsDraft draft,
        CancellationToken cancellationToken = default)
    {
        var picId = draft.ImageGalleryPicId ?? NewsArticleImage.TryParseGalleryPicId(draft.ImageBlobKey);
        if (picId is not int id)
        {
            return null;
        }

        var photo = await photos.GetByIdAsync(id, cancellationToken);
        return photo?.ImageUrl;
    }

    public static async Task<string?> ValidatePicAsync(
        IAdminPhotoRepository photos,
        IFormFile? uploadedFile,
        int? imageGalleryPicId,
        CancellationToken cancellationToken = default)
    {
        if (uploadedFile is { Length: > 0 } || imageGalleryPicId is not int picId)
        {
            return null;
        }

        var photo = await photos.GetByIdAsync(picId, cancellationToken);
        return photo is null ? "That gallery photo was not found." : null;
    }
}
