using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Forum;

public sealed class TopicModel(IForumRepository forumRepository) : ForumTopicPageModel(forumRepository)
{
    public async Task<IActionResult> OnGetAsync(int topicId, string slug, CancellationToken cancellationToken) =>
        await LoadTopicPageAsync(topicId, slug, 1, cancellationToken);
}