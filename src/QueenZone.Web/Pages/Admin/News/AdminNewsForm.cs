using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class AdminNewsForm
{
    [FromForm(Name = "title")]
    public string Title { get; init; } = string.Empty;

    [FromForm(Name = "slug")]
    public string? Slug { get; init; }

    [FromForm(Name = "excerpt")]
    public string Excerpt { get; init; } = string.Empty;

    [FromForm(Name = "body")]
    public string Body { get; init; } = string.Empty;

    [FromForm(Name = "publishedAt")]
    public string PublishedAt { get; init; } = string.Empty;

    [FromForm(Name = "sourceUrl")]
    public string? SourceUrl { get; init; }

    [FromForm(Name = "imageBlobKey")]
    public string? ImageBlobKey { get; init; }

    [FromForm(Name = "imageGalleryPicId")]
    public int? ImageGalleryPicId { get; init; }

    [FromForm(Name = "articleImage")]
    public IFormFile? ArticleImage { get; set; }

    [FromForm(Name = "cropX")]
    public string? CropX { get; init; }

    [FromForm(Name = "cropY")]
    public string? CropY { get; init; }

    [FromForm(Name = "cropWidth")]
    public string? CropWidth { get; init; }

    [FromForm(Name = "cropHeight")]
    public string? CropHeight { get; init; }

    public NewsArticleImageCrop? ToCrop()
    {
        if (!TryParseCropPart(CropX, out var x)
            || !TryParseCropPart(CropY, out var y)
            || !TryParseCropPart(CropWidth, out var width)
            || !TryParseCropPart(CropHeight, out var height))
        {
            return null;
        }

        return new NewsArticleImageCrop(x, y, width, height);
    }

    public AdminNewsDraft ToDraft()
    {
        DateTime publishedAt = default;
        if (!string.IsNullOrWhiteSpace(PublishedAt)
            && DateTime.TryParse(PublishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            publishedAt = parsed;
        }

        var galleryPicId = ImageGalleryPicId is > 0 ? ImageGalleryPicId : null;
        var imageBlobKey = string.IsNullOrWhiteSpace(ImageBlobKey) ? null : ImageBlobKey.Trim();
        if (galleryPicId is int picId)
        {
            imageBlobKey = NewsArticleImage.ToGalleryReference(picId);
        }

        return new AdminNewsDraft(
            (Title ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(Slug) ? null : Slug.Trim(),
            (Excerpt ?? string.Empty).Trim(),
            Body ?? string.Empty,
            publishedAt,
            string.IsNullOrWhiteSpace(SourceUrl) ? null : SourceUrl.Trim(),
            imageBlobKey,
            galleryPicId);
    }

    private static bool TryParseCropPart(string? value, out int parsed)
    {
        parsed = 0;
        return !string.IsNullOrWhiteSpace(value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }
}
