using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class NewModel : AdminNewsPageModel
{
    public ArticleFormViewModel Form { get; private set; } = BuildForm(
        new AdminNewsDraft(string.Empty, null, string.Empty, string.Empty, DateTime.UtcNow.Date, null),
        null);

    public void OnGet()
    {
        ViewData["Title"] = "Create news article";
    }

    public static ArticleFormViewModel BuildForm(AdminNewsDraft draft, IReadOnlyList<string>? errors) =>
        new("Create news article", "/admin/news", draft, errors);
}
