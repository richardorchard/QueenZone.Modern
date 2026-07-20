using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class IndexModel(
    IArticlesRepository articlesRepository,
    IArticleRepository articleRepository,
    PublicQueryCacheService publicQueryCache) : ArticlesArchivePageModel(articlesRepository, publicQueryCache)
{
    public async Task<IActionResult> OnGetAsync(
        [Microsoft.AspNetCore.Mvc.FromQuery(Name = "cp")] int communityPage = 1,
        [Microsoft.AspNetCore.Mvc.FromQuery(Name = "tag")] string? tag = null,
        CancellationToken cancellationToken = default) =>
        await LoadArchivePageAsync(1, cancellationToken, articleRepository, communityPage, tag);
}
