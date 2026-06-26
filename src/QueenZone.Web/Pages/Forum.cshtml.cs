using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class ForumModel(IForumRepository forumRepository) : PageModel
{
    public ForumArchiveStats Stats { get; private set; } = new(0, 0, 0);

    public IReadOnlyList<ForumCategoryItem> Categories { get; private set; } = Array.Empty<ForumCategoryItem>();

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Forum", "/forum")];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await forumRepository.GetCategoriesAsync(cancellationToken);
        var threadCount = await forumRepository.GetTotalThreadCountAsync(cancellationToken);
        Stats = ForumArchiveStats.FromCategories(Categories, threadCount);
    }
}