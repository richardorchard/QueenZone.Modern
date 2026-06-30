using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Account;

public sealed class ExternalLoginModel : PageModel
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        MemberAuthenticationSchemes.Google,
        MemberAuthenticationSchemes.Microsoft,
        MemberAuthenticationSchemes.Facebook,
    };

    public IActionResult OnGet(string provider, string? returnUrl)
    {
        if (!SupportedProviders.Contains(provider))
        {
            return NotFound();
        }

        var callbackUrl = Url.Page("/Account/ExternalLoginCallback", pageHandler: null, values: new { returnUrl })!;
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        return Challenge(properties, provider);
    }
}
