using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Admin;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => Redirect("/admin/news");
}
