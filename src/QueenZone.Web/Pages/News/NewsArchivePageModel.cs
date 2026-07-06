using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public abstract class NewsArchivePageModel(
    INewsRepository newsRepository,
    PublicQueryCacheService publicQueryCache) : PageModel
{
    public IReadOnlyList<NewsItem> Items { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    protected async Task<IActionResult> LoadArchivePageAsync(int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var publishedCount = await publicQueryCache.GetNewsPublishedCountAsync(cancellationToken);
        var archive = await newsRepository.GetArchivePageAsync(page, NewsRoutes.ArchivePageSize, cancellationToken);
        var totalPages = NewsRoutes.ResolveArchiveTotalPages(
            page,
            archive.Count,
            publishedCount,
            NewsRoutes.GetArchiveTotalPages(publishedCount));

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

        Items = archive;
        CurrentPage = page;
        TotalPages = totalPages;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("News", "/news")];

        ViewData["Title"] = NewsRoutes.GetArchivePageTitle(page);
        ViewData["CanonicalPath"] = NewsRoutes.GetArchiveCanonicalPath(page);
        if (page > 1)
        {
            ViewData["PrevPath"] = NewsRoutes.GetArchiveCanonicalPath(page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = NewsRoutes.GetArchiveCanonicalPath(page + 1);
        }

        return Page();
    }
}
