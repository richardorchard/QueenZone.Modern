using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class SearchModel(ISiteSearchService siteSearchService) : PageModel
{
    public const int PageSize = 20;

    [BindProperty(Name = "q", SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(Name = "type", SupportsGet = true)]
    public string? TypeFilter { get; set; }

    // [FromQuery], not [BindProperty(SupportsGet)]: Razor Pages reserves the route-value key
    // "page" for its own page-selection machinery, which otherwise wins over the same-named
    // query-string value in the composite binding source and silently leaves this at its default.
    [FromQuery(Name = "page")]
    public int CurrentPage { get; set; } = 1;

    /// <summary>Normalized, validated form of <see cref="TypeFilter"/> — null means "all types".</summary>
    public string? ActiveContentType { get; private set; }

    public SiteSearchPage? Results { get; private set; }

    public int TotalPages { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["CanonicalPath"] = "/search";

        ActiveContentType = SiteSearchContentType.Normalize(TypeFilter);

        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }

        CurrentPage = Math.Max(1, CurrentPage);
        Results = await siteSearchService.SearchAsync(Query, ActiveContentType, CurrentPage, PageSize, cancellationToken);
        TotalPages = ArchivePagination.GetTotalPages(Results.TotalCount, PageSize);
    }
}
