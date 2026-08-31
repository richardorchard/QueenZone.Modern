using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Timeline;

public sealed class EditPostModel(
    IAdminQueenHistoryRepository historyRepository,
    IOutputCacheStore outputCacheStore,
    PublicQueryCacheService publicQueryCache) : AdminTimelinePageModel
{
    public TimelineFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        int id,
        [FromForm] AdminTimelineForm form,
        CancellationToken cancellationToken)
    {
        var existing = await historyRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var draft = form.ToDraft();
        var errors = QueenHistoryValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Edit timeline event";
            Form = EditModel.BuildForm(existing, draft, errors);
            return Page();
        }

        try
        {
            await historyRepository.UpdateAsync(id, draft, form.ParseRowVersion(), cancellationToken);
        }
        catch (OptimisticConcurrencyException)
        {
            var current = await historyRepository.GetByIdAsync(id, cancellationToken) ?? existing;
            ViewData["Title"] = "Edit timeline event";
            Form = EditModel.BuildForm(
                current,
                ToDraft(current),
                [OptimisticConcurrencyException.UserMessage]);
            return Page();
        }

        await IndexModel.InvalidatePublicHistoryCacheAsync(outputCacheStore, publicQueryCache, cancellationToken);

        TempData[MessageKey] = "Saved timeline event.";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/timeline/{id}/edit");
    }
}
