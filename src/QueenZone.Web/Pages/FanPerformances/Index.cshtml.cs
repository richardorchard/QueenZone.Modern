using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QueenZone.Web.Pages.FanPerformances;

[EnableRateLimiting(FanPerformanceRateLimitingOptions.BrowsePolicy)]
public sealed class IndexModel(
    PublicQueryCacheService publicQueryCache,
    FanPerformanceCreditResolver creditResolver)
    : FanPerformanceArchivePageModel(publicQueryCache, creditResolver)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
