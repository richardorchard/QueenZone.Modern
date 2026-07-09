using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Forum;

public abstract class ForumTopicPageModel(IForumRepository forumRepository) : PageModel
{
    public ForumThreadHeader? Header { get; private set; }

    public IReadOnlyList<ForumPostViewModel> Posts { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalPosts { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

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

        var header = PublicContentMapper.ToForumThreadHeader(topicPage.Header);
        var canonicalSlug = NewsSlug.Slugify(topicPage.Header.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(ForumRoutes.GetTopicCanonicalPath(header, page));
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

        Header = header;
        Posts = PublicContentMapper.ToForumPostViewModels(topicPage.Posts);
        CurrentPage = page;
        TotalPages = totalPages;
        TotalPosts = topicPage.TotalCount;
        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Forum", "/forum"),
            new BreadcrumbItem(header.ForumName, header.CategoryPath),
            new BreadcrumbItem(header.Title, header.DetailPath),
        ];

        ViewData["Title"] = ForumRoutes.GetTopicPageTitle(header, page);
        ViewData["CanonicalPath"] = ForumRoutes.GetTopicCanonicalPath(header, page);
        ViewData["Description"] = $"Read-only Queenzone forum archive thread in {header.ForumName}.";

        if (page > 1)
        {
            ViewData["PrevPath"] = ForumRoutes.GetTopicCanonicalPath(header, page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = ForumRoutes.GetTopicCanonicalPath(header, page + 1);
        }

        return Page();
    }
}
