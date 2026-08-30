using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Trivia;

public sealed class EditPostModel(
    ITriviaRepository triviaRepository,
    IOutputCacheStore outputCacheStore) : AdminTriviaPageModel
{
    public TriviaFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        int id,
        [FromForm] AdminTriviaForm form,
        CancellationToken cancellationToken)
    {
        var existing = await triviaRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var draft = form.ToDraft();
        var errors = TriviaValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Edit trivia fact";
            Form = EditModel.BuildForm(existing, draft, errors);
            return Page();
        }

        await triviaRepository.UpdateAsync(id, draft, cancellationToken);
        await IndexModel.InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);

        TempData[MessageKey] = "Saved trivia fact.";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/trivia/{id}/edit");
    }
}
