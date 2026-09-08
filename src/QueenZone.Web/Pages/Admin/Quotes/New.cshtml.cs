using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed class NewModel : AdminQuotePageModel
{
    public QuoteFormViewModel Form { get; private set; } = BuildForm(
        new AdminQuoteDraft(string.Empty, string.Empty, true, null),
        null);

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Add quote";
        Breadcrumbs = AdminBreadcrumbs.Page("Quotes", "/admin/quotes", "New quote");
    }

    public static QuoteFormViewModel BuildForm(AdminQuoteDraft draft, IReadOnlyList<string>? errors) =>
        new("Add quote", "/admin/quotes", draft, errors);
}
