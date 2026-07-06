using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class IndexModel(
    INewsRepository newsRepository,
    PublicQueryCacheService publicQueryCache) : NewsArchivePageModel(newsRepository, publicQueryCache)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
