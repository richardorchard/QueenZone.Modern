using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FanPerformances;

[EnableRateLimiting(FanPerformanceRateLimitingOptions.BrowsePolicy)]
public sealed class IndexModel(IFanPerformanceRepository fanPerformanceRepository) : FanPerformanceArchivePageModel(fanPerformanceRepository)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
