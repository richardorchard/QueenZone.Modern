using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class DetailModel(
    INewsRepository newsRepository,
    NewsDiscussionComposer newsDiscussion,
    IOptions<SiteOptions> siteOptions) : PageModel
{
    public NewsDetailItem? Item { get; private set; }

    public string StructuredDataJson { get; private set; } = string.Empty;

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var item = await newsRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var detail = await newsDiscussion.ToDetailItemAsync(item, cancellationToken);
        var canonicalSlug = NewsSlug.ResolveForArticle(item);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(detail.DetailPath);
        }

        var canonicalPath = NewsArticleContent.GetDetailCanonicalPath(detail.Id, detail.Title, item.Slug);

        Item = detail;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("News", "/news"), new BreadcrumbItem(detail.Title, detail.DetailPath)];
        ViewData["Title"] = $"{detail.Title} | QueenZone news";
        ViewData["CanonicalPath"] = canonicalPath;
        ViewData["Description"] = detail.Excerpt;

        StructuredDataJson = EditorialJsonLd.BuildNewsArticle(
            detail.Title,
            canonicalPath,
            detail.PublishedAt,
            detail.Excerpt,
            siteOptions.Value.PublicBaseUrl);

        return Page();
    }
}
