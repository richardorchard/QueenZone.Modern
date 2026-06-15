using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class DetailModel(INewsRepository newsRepository) : PageModel
{
    public NewsItem? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var item = await newsRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var canonicalSlug = NewsRoutes.Slugify(item.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(NewsRoutes.GetNewsDetailPath(item));
        }

        Item = item;
        ViewData["Title"] = $"{item.Title} | QueenZone news";
        ViewData["CanonicalPath"] = NewsArticleContent.GetDetailCanonicalPath(item.Id, item.Title);
        ViewData["Description"] = item.Excerpt;

        return Page();
    }
}
