using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Web.Archive;

namespace QueenZone.Web.Pages.News;

public abstract class NewsArchivePageModel(
    INewsRepository newsRepository,
    PublicQueryCacheService publicQueryCache) : PageModel
{
    public IReadOnlyList<NewsArchiveItem> Items { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    protected async Task<IActionResult> LoadArchivePageAsync(int page, CancellationToken cancellationToken)
    {
        var result = await ArchivePageLoader.LoadAsync<NewsItem>(
            page,
            NewsRoutes.ArchivePageSize,
            ct => publicQueryCache.GetNewsPublishedCountAsync(ct),
            (p, size, ct) => newsRepository.GetArchivePageAsync(p, size, ct),
            (p, ic, pc, tp) => NewsRoutes.ResolveArchiveTotalPages(p, ic, pc, tp),
            (p, total) => new ArchivePageContext(
                p, total,
                NewsRoutes.GetArchivePageTitle(p),
                NewsRoutes.GetArchiveCanonicalPath(p),
                p > 1 ? NewsRoutes.GetArchiveCanonicalPath(p - 1) : null,
                total > 0 && p < total ? NewsRoutes.GetArchiveCanonicalPath(p + 1) : null),
            cancellationToken);

        if (result is ArchivePageResult<NewsItem>.NotFound)
            return NotFound();

        var success = (ArchivePageResult<NewsItem>.Success)result;
        var ctx = success.Context;

        Items = PublicContentMapper.ToNewsArchiveItems(success.Items);
        CurrentPage = ctx.CurrentPage;
        TotalPages = ctx.TotalPages;
        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("News", "/news")];

        ViewData["Title"] = ctx.Title;
        if (page <= 1)
        {
            ViewData["Description"] = "The latest Queen news and stories from QueenZone.";
            ViewData["RssFeedPath"] = NewsRoutes.FeedPath;
            ViewData["RssFeedTitle"] = "QueenZone News";
        }

        ViewData["CanonicalPath"] = ctx.CanonicalPath;
        if (ctx.PrevPath is not null)
            ViewData["PrevPath"] = ctx.PrevPath;
        if (ctx.NextPath is not null)
            ViewData["NextPath"] = ctx.NextPath;

        return Page();
    }
}
