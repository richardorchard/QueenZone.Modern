using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Stories;

public sealed class ArchivePageModel(IStoriesRepository storiesRepository) : StoriesArchivePageModel(storiesRepository)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent("/stories");
        }

        return await LoadArchivePageAsync(PageNumber, cancellationToken);
    }
}