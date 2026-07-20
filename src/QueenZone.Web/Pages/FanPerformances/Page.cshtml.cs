using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FanPerformances;

[EnableRateLimiting(FanPerformanceRateLimitingOptions.BrowsePolicy)]
public sealed class ArchivePageModel(IFanPerformanceRepository fanPerformanceRepository) : FanPerformanceArchivePageModel(fanPerformanceRepository)
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
