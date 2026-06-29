using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FanPerformances;

public sealed class IndexModel(IFanPerformanceRepository fanPerformanceRepository) : FanPerformanceArchivePageModel(fanPerformanceRepository)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
