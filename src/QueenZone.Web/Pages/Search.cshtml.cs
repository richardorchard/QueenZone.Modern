using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class SearchModel(IForumRepository forumRepository) : PageModel
{
    public const int PageSize = 20;

    [BindProperty(Name = "q", SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(Name = "page", SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public ForumSearchPage? Results { get; private set; }

    public int TotalPages { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }

        CurrentPage = Math.Max(1, CurrentPage);
        Results = await forumRepository.SearchForumAsync(Query, CurrentPage, PageSize, cancellationToken);
        TotalPages = ArchivePagination.GetTotalPages(Results.TotalCount, PageSize);
    }
}
