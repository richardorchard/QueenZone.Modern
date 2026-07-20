using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class ArchivePageModel(
    IArticlesRepository articlesRepository,
    PublicQueryCacheService publicQueryCache) : ArticlesArchivePageModel(articlesRepository, publicQueryCache)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent("/articles");
        }

        return await LoadArchivePageAsync(PageNumber, cancellationToken);
    }
}
