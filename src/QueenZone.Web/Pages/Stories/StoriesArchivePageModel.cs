using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Stories;

public abstract class StoriesArchivePageModel(IStoriesRepository storiesRepository) : PageModel
{
    public IReadOnlyList<StoryItem> Items { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    protected async Task<IActionResult> LoadArchivePageAsync(int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var publishedCount = await storiesRepository.GetPublishedCountAsync(cancellationToken);
        var archive = await storiesRepository.GetArchivePageAsync(page, StoriesRoutes.ArchivePageSize, cancellationToken);
        var totalPages = StoriesRoutes.ResolveArchiveTotalPages(
            page,
            archive.Count,
            publishedCount,
            StoriesRoutes.GetArchiveTotalPages(publishedCount));

        if (totalPages == 0)
        {
            if (page > 1)
            {
                return NotFound();
            }
        }
        else if (page > totalPages)
        {
            return NotFound();
        }

        Items = archive;
        CurrentPage = page;
        TotalPages = totalPages;

        ViewData["Title"] = StoriesRoutes.GetArchivePageTitle(page);
        ViewData["CanonicalPath"] = StoriesRoutes.GetArchiveCanonicalPath(page);
        if (page > 1)
        {
            ViewData["PrevPath"] = StoriesRoutes.GetArchiveCanonicalPath(page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = StoriesRoutes.GetArchiveCanonicalPath(page + 1);
        }

        return Page();
    }
}