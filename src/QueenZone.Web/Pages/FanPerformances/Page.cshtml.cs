using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FanPerformances;

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
