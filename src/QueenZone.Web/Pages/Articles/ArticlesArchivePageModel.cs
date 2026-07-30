using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using QueenZone.Data;
using QueenZone.Web.Archive;

namespace QueenZone.Web.Pages.Articles;

public abstract class ArticlesArchivePageModel(
    IArticlesRepository articlesRepository,
    PublicQueryCacheService publicQueryCache) : PageModel
{
    public const int CommunityPageSize = 12;

    public IReadOnlyList<ArticleArchiveItem> Items { get; private set; } = [];

    public IReadOnlyList<PublishedArticleSubmission> CommunityItems { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int CommunityCurrentPage { get; private set; }

    public int CommunityTotalPages { get; private set; }

    public string? ActiveTag { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    protected async Task<IActionResult> LoadArchivePageAsync(
        int page,
        CancellationToken cancellationToken,
        IArticleRepository? articleRepository = null,
        int communityPage = 1,
        string? tag = null)
    {
        var result = await ArchivePageLoader.LoadAsync<ArticleItem>(
            page,
            ArticlesRoutes.ArchivePageSize,
            ct => publicQueryCache.GetArticlePublishedCountAsync(ct),
            (p, size, ct) => articlesRepository.GetArchivePageAsync(p, size, ct),
            (p, ic, pc, tp) => ArticlesRoutes.ResolveArchiveTotalPages(p, ic, pc, tp),
            (p, total) => new ArchivePageContext(
                p, total,
                ArticlesRoutes.GetArchivePageTitle(p),
                ArticlesRoutes.GetArchiveCanonicalPath(p),
                p > 1 ? ArticlesRoutes.GetArchiveCanonicalPath(p - 1) : null,
                total > 0 && p < total ? ArticlesRoutes.GetArchiveCanonicalPath(p + 1) : null),
            cancellationToken);

        if (result is ArchivePageResult<ArticleItem>.NotFound)
            return NotFound();

        var success = (ArchivePageResult<ArticleItem>.Success)result;
        var ctx = success.Context;

        Items = PublicContentMapper.ToArticleArchiveItems(success.Items);
        CurrentPage = ctx.CurrentPage;
        TotalPages = ctx.TotalPages;
        ActiveTag = tag;

        if (articleRepository is not null)
        {
            try
            {
                communityPage = Math.Max(1, communityPage);
                var communityCount = await articleRepository.GetCountAsync(tag, cancellationToken);
                CommunityTotalPages = (int)Math.Ceiling(communityCount / (double)CommunityPageSize);
                if (CommunityTotalPages > 0 && communityPage > CommunityTotalPages)
                {
                    return NotFound();
                }

                CommunityItems = await articleRepository.GetPageAsync(communityPage, CommunityPageSize, tag, cancellationToken);
                CommunityCurrentPage = communityPage;
            }
            catch (SqlException)
            {
                // Community articles are additive; archive pages must still render when the
                // modern submission table is unavailable (e.g. migration not yet applied).
                CommunityItems = [];
                CommunityCurrentPage = 1;
                CommunityTotalPages = 0;
            }
        }

        Breadcrumbs = [BreadcrumbItem.Home, new BreadcrumbItem("Articles", "/articles")];

        ViewData["Title"] = ctx.Title;
        if (page <= 1)
        {
            ViewData["Description"] = "In-depth Queen articles and interviews from the Queenzone.com archive.";
            ViewData["RssFeedPath"] = ArticlesRoutes.FeedPath;
            ViewData["RssFeedTitle"] = "QueenZone Articles";
        }

        ViewData["CanonicalPath"] = ctx.CanonicalPath;
        if (ctx.PrevPath is not null)
            ViewData["PrevPath"] = ctx.PrevPath;
        if (ctx.NextPath is not null)
            ViewData["NextPath"] = ctx.NextPath;

        return Page();
    }
}
