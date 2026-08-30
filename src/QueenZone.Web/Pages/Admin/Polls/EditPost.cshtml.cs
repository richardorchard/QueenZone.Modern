using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Polls;

public sealed class EditPostModel(
    IHomePollRepository homePollRepository,
    IOutputCacheStore outputCacheStore) : AdminPollPageModel
{
    public PollFormViewModel? Form { get; private set; }

    public HomePollAdminDetail? Poll { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        Guid id,
        [FromForm] AdminPollForm form,
        CancellationToken cancellationToken)
    {
        var existing = await homePollRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var draft = form.ToDraft();
        var errors = HomePollValidation.ValidateDraft(draft).ToList();
        if (existing.VoteCount > 0)
        {
            errors.Add("Question and options cannot be changed after the first vote.");
        }

        if (errors.Count > 0)
        {
            ViewData["Title"] = "Edit poll";
            Poll = existing;
            Form = EditModel.BuildForm(existing, draft, errors);
            return Page();
        }

        try
        {
            await homePollRepository.UpdateAsync(id, draft, cancellationToken);
        }
        catch (HomePollException ex)
        {
            ViewData["Title"] = "Edit poll";
            Poll = existing;
            Form = EditModel.BuildForm(existing, draft, [ex.Message]);
            return Page();
        }

        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = "Saved poll.";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/polls/{id}/edit");
    }
}
