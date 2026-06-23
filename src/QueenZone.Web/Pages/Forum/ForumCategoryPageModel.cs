using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Forum;

public abstract class ForumCategoryPageModel(IForumRepository forumRepository) : PageModel
{
    public ForumCategoryItem? Category { get; private set; }

    public IReadOnlyList<ForumTopicItem> Topics { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalTopics { get; private set; }

    protected async Task<IActionResult> LoadCategoryPageAsync(
        int id,
        string slug,
        int page,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var category = await forumRepository.GetCategoryByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var canonicalSlug = NewsSlug.Slugify(category.Name);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(ForumRoutes.GetCategoryCanonicalPath(category, page));
        }

        var topicsPage = await forumRepository.GetCategoryTopicsPageAsync(
            id,
            page,
            ForumRoutes.TopicsPageSize,
            cancellationToken);

        var totalPages = ForumRoutes.GetTopicsTotalPages(topicsPage.TotalCount, ForumRoutes.TopicsPageSize);
        if (topicsPage.TotalCount == 0)
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

        Category = category;
        Topics = topicsPage.Topics;
        CurrentPage = page;
        TotalPages = totalPages;
        TotalTopics = topicsPage.TotalCount;

        ViewData["Title"] = ForumRoutes.GetCategoryPageTitle(category, page);
        ViewData["CanonicalPath"] = ForumRoutes.GetCategoryCanonicalPath(category, page);
        ViewData["Description"] = string.IsNullOrWhiteSpace(category.Description)
            ? $"Read-only Queenzone forum archive for {category.Name}."
            : category.Description;

        if (page > 1)
        {
            ViewData["PrevPath"] = ForumRoutes.GetCategoryCanonicalPath(category, page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = ForumRoutes.GetCategoryCanonicalPath(category, page + 1);
        }

        return Page();
    }
}