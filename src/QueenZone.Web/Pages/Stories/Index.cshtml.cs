using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Stories;

public sealed class IndexModel(IStoriesRepository storiesRepository) : StoriesArchivePageModel(storiesRepository)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}