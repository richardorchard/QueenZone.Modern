using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QueenZone.Web.Pages.FanPerformances;

[EnableRateLimiting(FanPerformanceRateLimitingOptions.BrowsePolicy)]
public sealed class ArchivePageModel(
    PublicQueryCacheService publicQueryCache,
    FanPerformanceCreditResolver creditResolver)
    : FanPerformanceArchivePageModel(publicQueryCache, creditResolver)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent(FanPerformanceRoutes.GetIndexPath());
        }

        return await LoadArchivePageAsync(PageNumber, cancellationToken);
    }
}
