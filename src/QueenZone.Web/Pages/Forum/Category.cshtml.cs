using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Forum;

public sealed class CategoryModel(IForumRepository forumRepository) : ForumCategoryPageModel(forumRepository)
{
    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken) =>
        await LoadCategoryPageAsync(id, slug, 1, cancellationToken);
}