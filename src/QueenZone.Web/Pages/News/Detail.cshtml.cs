using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class DetailModel(INewsRepository newsRepository) : PageModel
{
    public NewsDetailItem? Item { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var item = await newsRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var detail = PublicContentMapper.ToNewsDetailItem(item);
        var canonicalSlug = NewsSlug.ResolveForArticle(item);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(detail.DetailPath);
        }

        Item = detail;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("News", "/news"), new BreadcrumbItem(detail.Title, detail.DetailPath)];
        ViewData["Title"] = $"{detail.Title} | QueenZone news";
        ViewData["CanonicalPath"] = NewsArticleContent.GetDetailCanonicalPath(detail.Id, detail.Title, item.Slug);
        ViewData["Description"] = detail.Excerpt;

        return Page();
    }
}
