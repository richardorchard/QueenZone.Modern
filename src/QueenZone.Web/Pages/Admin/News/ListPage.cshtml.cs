using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class ListPageModel(IAdminNewsRepository adminNewsRepository)
    : AdminNewsListPageModel(adminNewsRepository)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber <= 1)
        {
            return Redirect(AdminNewsRoutes.GetListPath(1));
        }

        return await LoadListPageAsync(PageNumber, cancellationToken);
    }
}
