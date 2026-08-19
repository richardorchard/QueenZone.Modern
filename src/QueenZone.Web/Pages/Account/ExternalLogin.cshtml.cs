using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Web.Infrastructure;

namespace QueenZone.Web.Pages.Account;

public sealed class ExternalLoginModel : PageModel
{
    public IActionResult OnGet(string provider, string? returnUrl)
    {
        if (MemberAuthenticationSchemes.NormalizeExternalProvider(provider) is null)
        {
            return NotFound();
        }

        var safeReturnUrl = LocalReturnUrl.Resolve(returnUrl);
        var callbackUrl = Url.Page(
            "/Account/ExternalLoginCallback",
            pageHandler: null,
            values: new { returnUrl = safeReturnUrl })!;
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        if (!string.Equals(provider, MemberAuthenticationSchemes.Apple, StringComparison.OrdinalIgnoreCase))
        {
            properties.SetParameter("prompt", "select_account");
        }

        return Challenge(properties, provider);
    }
}
