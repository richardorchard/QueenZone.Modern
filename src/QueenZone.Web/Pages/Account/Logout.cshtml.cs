using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Account;

public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await SignOutMemberAsync();
        return RedirectToSignedOutLogin();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await SignOutMemberAsync();
        return RedirectToSignedOutLogin();
    }

    private async Task SignOutMemberAsync()
    {
        await HttpContext.SignOutAsync(MemberAuthenticationSchemes.MembersCookie);
        await HttpContext.SignOutAsync(MemberAuthenticationSchemes.ExternalCookie);
    }

    private RedirectResult RedirectToSignedOutLogin() =>
        Redirect("/account/login?signedOut=1");
}
