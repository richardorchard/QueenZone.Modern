using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed class EditModel(IQuoteRepository quoteRepository) : AdminQuotePageModel
{
    public QuoteFormViewModel? Form { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetByIdAsync(id, cancellationToken);
        if (quote is null)
        {
            return NotFound();
        }

        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Edit quote";
        Form = BuildForm(quote, ToDraft(quote), null);
        return Page();
    }

    public static QuoteFormViewModel BuildForm(
        QuoteItem quote,
        AdminQuoteDraft draft,
        IReadOnlyList<string>? errors) =>
        new(
            "Edit quote",
            $"/admin/quotes/{quote.Id}",
            draft,
            errors,
            quote);
}
