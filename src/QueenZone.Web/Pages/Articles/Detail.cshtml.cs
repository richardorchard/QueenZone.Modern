using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class DetailModel(IArticlesRepository articlesRepository) : PageModel
{
    public ArticleItem? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var item = await articlesRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var canonicalSlug = NewsSlug.Slugify(item.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(ArticlesRoutes.GetArticleDetailPath(item));
        }

        Item = item;
        ViewData["Title"] = $"{item.Title} | QueenZone articles";
        ViewData["CanonicalPath"] = ArticleContent.GetDetailCanonicalPath(item.Id, item.Title);
        ViewData["Description"] = item.Excerpt;

        return Page();
    }
}