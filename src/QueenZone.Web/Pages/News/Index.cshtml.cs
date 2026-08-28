using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class IndexModel(
    INewsRepository newsRepository,
    PublicQueryCacheService publicQueryCache,
    NewsDiscussionComposer newsDiscussion) : NewsArchivePageModel(newsRepository, publicQueryCache, newsDiscussion)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(1, cancellationToken);
}
