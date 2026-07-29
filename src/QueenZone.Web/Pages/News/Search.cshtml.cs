using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

public sealed class NewsSearchModel(INewsRepository newsRepository) : PageModel
{
    public const int PageSize = 20;

    [BindProperty(Name = "q", SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(Name = "page", SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<NewsArchiveItem> Items { get; private set; } = [];

    public int TotalCount { get; private set; }

    public int TotalPages { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } =
        [BreadcrumbItem.Home, new BreadcrumbItem("News", "/news")];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(Query);

        ViewData["Title"] = hasQuery ? $"News search: {Query}" : "News search";
        ViewData["Description"] = "Search the QueenZone news archive by keyword.";
        ViewData["CanonicalPath"] = hasQuery
            ? $"/news/search?q={Uri.EscapeDataString(Query!.Trim())}"
            : "/news/search";

        if (!hasQuery)
        {
            return Page();
        }

        CurrentPage = Math.Max(1, CurrentPage);

        var results = await newsRepository.SearchAsync(Query!, CurrentPage, PageSize, cancellationToken);

        Items = PublicContentMapper.ToNewsArchiveItems(results.Items);
        TotalCount = results.TotalCount;
        TotalPages = ArchivePagination.GetTotalPages(results.TotalCount, PageSize);

        return Page();
    }
}
