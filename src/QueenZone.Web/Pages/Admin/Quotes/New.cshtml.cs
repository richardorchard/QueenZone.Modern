using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed class NewModel : AdminQuotePageModel
{
    public QuoteFormViewModel Form { get; private set; } = BuildForm(
        new AdminQuoteDraft(string.Empty, string.Empty, true, null),
        null);

    public void OnGet()
    {
        ViewData["Title"] = "Add quote";
    }

    public static QuoteFormViewModel BuildForm(AdminQuoteDraft draft, IReadOnlyList<string>? errors) =>
        new("Add quote", "/admin/quotes", draft, errors);
}
