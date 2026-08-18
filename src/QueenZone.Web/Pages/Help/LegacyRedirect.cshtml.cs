using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Help;

public sealed class LegacyRedirectModel : PageModel
{
    public IActionResult OnGet(string? path)
    {
        var target = string.IsNullOrWhiteSpace(path)
            ? "/contact"
            : "/contact/" + path.Trim('/');
        return RedirectPermanent(target);
    }
}
