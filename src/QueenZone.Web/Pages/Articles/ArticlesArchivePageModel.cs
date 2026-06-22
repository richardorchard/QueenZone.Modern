using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public abstract class ArticlesArchivePageModel(IArticlesRepository articlesRepository) : PageModel
{
    public IReadOnlyList<ArticleItem> Items { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    protected async Task<IActionResult> LoadArchivePageAsync(int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var publishedCount = await articlesRepository.GetPublishedCountAsync(cancellationToken);
        var archive = await articlesRepository.GetArchivePageAsync(page, ArticlesRoutes.ArchivePageSize, cancellationToken);
        var totalPages = ArticlesRoutes.ResolveArchiveTotalPages(
            page,
            archive.Count,
            publishedCount,
            ArticlesRoutes.GetArchiveTotalPages(publishedCount));

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

        ViewData["Title"] = ArticlesRoutes.GetArchivePageTitle(page);
        ViewData["CanonicalPath"] = ArticlesRoutes.GetArchiveCanonicalPath(page);
        if (page > 1)
        {
            ViewData["PrevPath"] = ArticlesRoutes.GetArchiveCanonicalPath(page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = ArticlesRoutes.GetArchiveCanonicalPath(page + 1);
        }

        return Page();
    }
}