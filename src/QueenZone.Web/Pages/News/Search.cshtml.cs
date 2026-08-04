using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.News;

/// <summary>
/// News search is now part of the unified <c>/search</c> page. This route is kept as a
/// permanent redirect (rather than removed) so existing inbound links and bookmarks to
/// <c>/news/search</c> keep working.
/// </summary>
public sealed class NewsSearchModel : PageModel
{
    [BindProperty(Name = "q", SupportsGet = true)]
    public string? Query { get; set; }

    // [FromQuery], not [BindProperty(SupportsGet)] — see SearchModel.CurrentPage for why.
    [FromQuery(Name = "page")]
    public int CurrentPage { get; set; } = 1;

    public IActionResult OnGet()
    {
        var target = $"/search?type={SiteSearchContentType.News}";
        if (!string.IsNullOrWhiteSpace(Query))
        {
            target += $"&q={Uri.EscapeDataString(Query.Trim())}";
        }

        if (CurrentPage > 1)
        {
            target += $"&page={CurrentPage}";
        }

        return RedirectPermanent(target);
    }
}
