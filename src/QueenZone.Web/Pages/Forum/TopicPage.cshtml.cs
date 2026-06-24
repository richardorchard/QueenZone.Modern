using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Forum;

public sealed class TopicPageModel(IForumRepository forumRepository) : ForumTopicPageModel(forumRepository)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(int topicId, string slug, CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent(ForumRoutes.GetTopicCanonicalPath(topicId, slug));
        }

        return await LoadTopicPageAsync(topicId, slug, PageNumber, cancellationToken);
    }
}