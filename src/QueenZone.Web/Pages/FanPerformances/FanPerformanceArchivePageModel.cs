using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FanPerformances;

public abstract class FanPerformanceArchivePageModel(PublicQueryCacheService publicQueryCache) : PageModel
{
    public FanPerformanceListViewModel PerformanceList { get; private set; } = FanPerformanceListViewModel.Empty;

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    protected async Task<IActionResult> LoadArchivePageAsync(int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var visibleCount = await publicQueryCache.GetFanPerformanceVisibleCountAsync(cancellationToken);
        var items = await publicQueryCache.GetFanPerformancePageAsync(page, FanPerformanceRoutes.PageSize, cancellationToken);
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

        PerformanceList = await BuildPerformanceListAsync(items, page, visibleCount, cancellationToken);
        CurrentPage = page;
        TotalPages = totalPages;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("Fan Performances", FanPerformanceRoutes.GetIndexPath())];

        ViewData["Title"] = page <= 1 ? "Fan Performances | QueenZone" : $"Fan Performances – Page {page} | QueenZone";
        if (page <= 1)
        {
            ViewData["Description"] = "Fan recordings of Queen songs, submitted by performers from the Queenzone community.";
        }

        ViewData["CanonicalPath"] = FanPerformanceRoutes.GetPagePath(page);

        return Page();
    }

    private async Task<FanPerformanceListViewModel> BuildPerformanceListAsync(
        IReadOnlyList<FanPerformance> items,
        int page,
        int visibleCount,
        CancellationToken cancellationToken)
    {
        var memberAuth = await HttpContext.AuthenticateMemberAsync();
        var isSignedIn = memberAuth.Succeeded;
        var loginReturnUrl = FanPerformanceRoutes.GetPagePath(page);

        var listItems = items
            .Select(performance => new FanPerformanceListItem(
                performance.Id,
                performance.Title,
                performance.PerformedBy,
                performance.Description,
                performance.DateAdded,
                isSignedIn ? FanPerformanceRoutes.GetAudioPath(performance.Id, performance.Title) : null))
            .ToList();

        IReadOnlyList<FanPerformanceCatalogEntry> catalog = [];
        if (isSignedIn && visibleCount > 0)
        {
            var catalogSource = items;
            if (page != 1 || items.Count < visibleCount)
            {
                catalogSource = await publicQueryCache.GetFanPerformancePageAsync(1, visibleCount, cancellationToken);
            }

            catalog = FanPerformanceListViewModel.CreateCatalog(catalogSource);
        }

        return new FanPerformanceListViewModel(listItems, loginReturnUrl, catalog);
    }
}
