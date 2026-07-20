using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class CommunityDetailModel(
    IArticleRepository articleRepository,
    UgcHtml ugcHtml,
    IOptions<SiteOptions> siteOptions) : PageModel
{
    public PublishedArticleSubmission? Item { get; private set; }

    public string FormattedBody { get; private set; } = string.Empty;

    public string? CoverImageUrl { get; private set; }

    public PublishedArticleSubmission? PreviousArticle { get; private set; }

    public PublishedArticleSubmission? NextArticle { get; private set; }

    public string StructuredDataJson { get; private set; } = string.Empty;

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        var item = await articleRepository.GetBySlugAsync(slug, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        Item = item;
        FormattedBody = ugcHtml.FormatForDisplay(item.Body);

        if (!string.IsNullOrWhiteSpace(item.CoverImageBlobPath))
        {
            CoverImageUrl = UgcProxyPaths.GetPath(QueenZone.Storage.BlobUploadContainers.Articles, item.CoverImageBlobPath);
        }

        var (prev, next) = await articleRepository.GetAdjacentAsync(item.PublishedAt, cancellationToken);
        PreviousArticle = prev;
        NextArticle = next;

        var canonicalPath = ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug);
        var baseUrl = siteOptions.Value.PublicBaseUrl.TrimEnd('/');

        StructuredDataJson = BuildStructuredData(item, baseUrl + canonicalPath, baseUrl);

        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Articles", "/articles"),
            new BreadcrumbItem(item.Title, canonicalPath),
        ];

        ViewData["Title"] = $"{item.Title} | QueenZone articles";
        ViewData["CanonicalPath"] = canonicalPath;
        ViewData["Description"] = item.Excerpt;

        return Page();
    }

    private static string BuildStructuredData(PublishedArticleSubmission item, string canonicalUrl, string baseUrl)
    {
        static string J(string? s) => System.Text.Json.JsonSerializer.Serialize(s ?? string.Empty);
        return
            "{\n" +
            "  \"@context\": \"https://schema.org\",\n" +
            "  \"@type\": \"Article\",\n" +
            "  \"headline\": " + J(item.Title) + ",\n" +
            "  \"description\": " + J(item.Excerpt ?? string.Empty) + ",\n" +
            "  \"datePublished\": \"" + item.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\",\n" +
            "  \"url\": " + J(canonicalUrl) + ",\n" +
            "  \"author\": {\n" +
            "    \"@type\": \"Person\",\n" +
            "    \"name\": " + J(item.AuthorDisplayName ?? "QueenZone Community") + "\n" +
            "  },\n" +
            "  \"publisher\": {\n" +
            "    \"@type\": \"Organization\",\n" +
            "    \"name\": \"QueenZone\",\n" +
            "    \"url\": " + J(baseUrl) + "\n" +
            "  }\n" +
            "}";
    }
}

