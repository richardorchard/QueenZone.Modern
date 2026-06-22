using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Stories;

public sealed class DetailModel(IStoriesRepository storiesRepository) : PageModel
{
    public StoryItem? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var item = await storiesRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var canonicalSlug = NewsSlug.Slugify(item.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(StoriesRoutes.GetStoryDetailPath(item));
        }

        Item = item;
        ViewData["Title"] = $"{item.Title} | QueenZone stories";
        ViewData["CanonicalPath"] = StoriesArticleContent.GetDetailCanonicalPath(item.Id, item.Title);
        ViewData["Description"] = item.Excerpt;

        return Page();
    }
}