using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Forum;

public sealed class CategoryPageModel(IForumRepository forumRepository) : ForumCategoryPageModel(forumRepository)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent(ForumRoutes.GetCategoryCanonicalPath(id, slug));
        }

        return await LoadCategoryPageAsync(id, slug, PageNumber, cancellationToken);
    }
}