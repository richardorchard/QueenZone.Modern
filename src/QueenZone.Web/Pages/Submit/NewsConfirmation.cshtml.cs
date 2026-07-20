using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Submit;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy, AuthenticationSchemes = MemberAuthenticationSchemes.MembersCookie)]
public sealed class NewsConfirmationModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Suggestion received";
    }
}
