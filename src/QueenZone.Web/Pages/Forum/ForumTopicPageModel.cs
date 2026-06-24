using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Forum;

public abstract class ForumTopicPageModel(IForumRepository forumRepository) : PageModel
{
    public ForumTopicHeader? Header { get; private set; }

    public IReadOnlyList<ForumPostItem> Posts { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalPosts { get; private set; }

    protected async Task<IActionResult> LoadTopicPageAsync(
        int topicId,
        string slug,
        int page,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var topicPage = await forumRepository.GetTopicPostsPageAsync(
            topicId,
            page,
            ForumRoutes.PostsPageSize,
            cancellationToken);
        if (topicPage is null)
        {
            return NotFound();
        }

        var canonicalSlug = NewsSlug.Slugify(topicPage.Header.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(ForumRoutes.GetTopicCanonicalPath(topicPage.Header, page));
        }

        var totalPages = ForumRoutes.GetPostsTotalPages(topicPage.TotalCount, ForumRoutes.PostsPageSize);
        if (topicPage.TotalCount == 0)
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

        Header = topicPage.Header;
        Posts = topicPage.Posts;
        CurrentPage = page;
        TotalPages = totalPages;
        TotalPosts = topicPage.TotalCount;

        ViewData["Title"] = ForumRoutes.GetTopicPageTitle(topicPage.Header, page);
        ViewData["CanonicalPath"] = ForumRoutes.GetTopicCanonicalPath(topicPage.Header, page);
        ViewData["Description"] = $"Read-only Queenzone forum archive thread in {topicPage.Header.ForumName.Trim()}.";

        if (page > 1)
        {
            ViewData["PrevPath"] = ForumRoutes.GetTopicCanonicalPath(topicPage.Header, page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = ForumRoutes.GetTopicCanonicalPath(topicPage.Header, page + 1);
        }

        return Page();
    }
}