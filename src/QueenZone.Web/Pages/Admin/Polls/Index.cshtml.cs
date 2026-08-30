using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Polls;

public sealed class IndexModel(
    IHomePollRepository homePollRepository,
    IOutputCacheStore outputCacheStore) : AdminPollPageModel
{
    public IReadOnlyList<HomePollAdminItem> Polls { get; private set; } = [];

    public PollFormViewModel? CreateForm { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Polls = await homePollRepository.GetAllAsync(cancellationToken);
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Home polls";
    }

    public async Task<IActionResult> OnPostAsync(
        [FromForm] AdminPollForm form,
        CancellationToken cancellationToken)
    {
        var draft = form.ToDraft();
        var errors = HomePollValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Add poll";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        await homePollRepository.CreateAsync(draft, Guid.Empty, cancellationToken);
        TempData[MessageKey] = "Added draft poll.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/polls");
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await homePollRepository.PublishAsync(id, cancellationToken);
            await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
            TempData[MessageKey] = "Published poll. It is now the Home poll.";
            TempData[MessageKindKey] = "success";
        }
        catch (HomePollException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
        }

        return Redirect("/admin/polls");
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await homePollRepository.CloseAsync(id, cancellationToken);
            await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
            TempData[MessageKey] = "Closed poll. Results stay on Home until you hide or replace it.";
            TempData[MessageKindKey] = "success";
        }
        catch (HomePollException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
        }

        return Redirect("/admin/polls");
    }

    public async Task<IActionResult> OnPostHideAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await homePollRepository.HideAsync(id, cancellationToken);
            await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
            TempData[MessageKey] = "Hidden poll. It is no longer on Home.";
            TempData[MessageKindKey] = "success";
        }
        catch (HomePollException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
        }

        return Redirect("/admin/polls");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await homePollRepository.DeleteAsync(id, cancellationToken);
            await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
            TempData[MessageKey] = "Deleted draft poll.";
            TempData[MessageKindKey] = "success";
        }
        catch (HomePollException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
        }

        return Redirect("/admin/polls");
    }
}
