using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class EditorialArticleForm
{
    public Guid? Id { get; set; }
    public int? LegacyArticleId { get; set; }
    public Guid? SourceSubmissionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string? Source { get; set; }
    public string? ImageBlobKey { get; set; }
    public int? ImageGalleryPicId { get; set; }
    public DateTime? PublishedAt { get; set; } = DateTime.UtcNow.Date;
    public IFormFile? ArticleImage { get; set; }
    public int? CropX { get; set; }
    public int? CropY { get; set; }
    public int? CropWidth { get; set; }
    public int? CropHeight { get; set; }

    public NewsArticleImageCrop? ToCrop() => CropX is { } x && CropY is { } y && CropWidth is { } w && CropHeight is { } h
        ? new NewsArticleImageCrop(x, y, w, h) : null;

    public EditorialArticleDraft ToDraft(string sanitizedBody, string? imageKey = null) => new(
        Id, LegacyArticleId, SourceSubmissionId, Title, Slug, Excerpt, sanitizedBody, AuthorName, Category,
        Tags, Source, imageKey ?? ImageBlobKey, new DateTimeOffset(DateTime.SpecifyKind(ResolvedPublishedAt, DateTimeKind.Utc)));

    public static EditorialArticleForm From(EditorialArticle x) => new()
    {
        Id = x.Id,
        LegacyArticleId = x.LegacyArticleId,
        SourceSubmissionId = x.SourceSubmissionId,
        Title = x.Title,
        Slug = x.Slug,
        Excerpt = x.Excerpt,
        Body = x.Body,
        AuthorName = x.AuthorName,
        Category = x.Category,
        Tags = x.Tags,
        Source = x.Source,
        ImageBlobKey = x.ImageBlobKey,
        PublishedAt = x.PublishedAt.UtcDateTime,
    };

    internal DateTime ResolvedPublishedAt => PublishedAt ?? DateTime.UtcNow.Date;
}
