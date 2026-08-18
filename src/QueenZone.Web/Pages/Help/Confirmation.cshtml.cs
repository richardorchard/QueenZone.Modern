using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Help;

public sealed class ConfirmationModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Message sent — Queenzone";
        ViewData["CanonicalPath"] = "/contact/confirmation";
    }
}
