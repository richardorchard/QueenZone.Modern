using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class DetailModel(IArticlesRepository articlesRepository) : PageModel
{
    public ArticleDetailItem? Item { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var item = await articlesRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var detail = PublicContentMapper.ToArticleDetailItem(item);
        var canonicalSlug = NewsSlug.Slugify(item.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(detail.DetailPath);
        }

        Item = detail;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("Articles", "/articles"), new BreadcrumbItem(detail.Title, detail.DetailPath)];
        ViewData["Title"] = $"{detail.Title} | QueenZone articles";
        ViewData["CanonicalPath"] = ArticleContent.GetDetailCanonicalPath(detail.Id, detail.Title);
        ViewData["Description"] = detail.Excerpt;

        return Page();
    }
}
