using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class IndexModel(INewsRepository newsRepository) : NewsArchivePageModel(newsRepository)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
