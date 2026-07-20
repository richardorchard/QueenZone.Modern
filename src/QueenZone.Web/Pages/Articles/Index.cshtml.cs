using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class IndexModel(
    IArticlesRepository articlesRepository,
    IArticleSubmissionRepository articleSubmissionRepository,
    PublicQueryCacheService publicQueryCache) : ArticlesArchivePageModel(articlesRepository, articleSubmissionRepository, publicQueryCache)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
