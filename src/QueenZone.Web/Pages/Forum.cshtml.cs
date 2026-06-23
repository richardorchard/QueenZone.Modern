using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class ForumModel(IForumRepository forumRepository) : PageModel
{
    public ForumArchiveStats Stats { get; private set; } = new(0, 0, 0);

    public IReadOnlyList<ForumCategoryItem> Categories { get; private set; } = Array.Empty<ForumCategoryItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await forumRepository.GetArchiveStatsAsync(cancellationToken);
        Categories = await forumRepository.GetCategoriesAsync(cancellationToken);
    }
}