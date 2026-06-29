using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FanPerformances;

public abstract class FanPerformanceArchivePageModel(IFanPerformanceRepository fanPerformanceRepository) : PageModel
{
    public IReadOnlyList<FanPerformance> Items { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    protected async Task<IActionResult> LoadArchivePageAsync(int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var visibleCount = await fanPerformanceRepository.GetVisibleCountAsync(cancellationToken);
        var items = await fanPerformanceRepository.GetPageAsync(page, FanPerformanceRoutes.PageSize, cancellationToken);
        var totalPages = FanPerformanceRoutes.ResolveTotalPages(
            page,
            items.Count,
            visibleCount,
            FanPerformanceRoutes.GetTotalPages(visibleCount));

        if (totalPages == 0)
        {
            if (page > 1)
            {
                return NotFound();
            }
        }
        else if (page > totalPages)
        {
            return NotFound();
        }

        Items = items;
        CurrentPage = page;
        TotalPages = totalPages;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("Fan Performances", FanPerformanceRoutes.GetIndexPath())];

        ViewData["Title"] = page <= 1 ? "Fan Performances | QueenZone" : $"Fan Performances – Page {page} | QueenZone";
        ViewData["CanonicalPath"] = FanPerformanceRoutes.GetPagePath(page);

        return Page();
    }
}
