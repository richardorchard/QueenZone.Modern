using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed record QuoteFormViewModel(
    string Title,
    string Action,
    AdminQuoteDraft Draft,
    IReadOnlyList<string>? Errors,
    QuoteItem? Quote = null);
