using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed class EditPostModel(
    IQuoteRepository quoteRepository,
    IOutputCacheStore outputCacheStore) : AdminQuotePageModel
{
    public QuoteFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        int id,
        [FromForm] AdminQuoteForm form,
        CancellationToken cancellationToken)
    {
        var existing = await quoteRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var draft = form.ToDraft();
        var errors = QuoteValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Edit quote";
            Form = EditModel.BuildForm(existing, draft, errors);
            return Page();
        }

        await quoteRepository.UpdateAsync(id, draft, cancellationToken);
        await IndexModel.InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);

        TempData[MessageKey] = "Saved quote.";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/quotes/{id}/edit");
    }
}
