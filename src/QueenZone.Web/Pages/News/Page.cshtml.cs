using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class ArchivePageModel(
    INewsRepository newsRepository,
    PublicQueryCacheService publicQueryCache,
    NewsDiscussionComposer newsDiscussion) : NewsArchivePageModel(newsRepository, publicQueryCache, newsDiscussion)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent("/news");
        }

        return await LoadArchivePageAsync(PageNumber, cancellationToken);
    }
}
