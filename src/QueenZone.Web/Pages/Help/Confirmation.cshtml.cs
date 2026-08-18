using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Help;

public sealed class ConfirmationModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Help request sent — Queenzone";
        ViewData["CanonicalPath"] = "/help/confirmation";
    }
}
