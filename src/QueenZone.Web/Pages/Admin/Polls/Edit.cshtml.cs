using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Polls;

public sealed class EditModel(IHomePollRepository homePollRepository) : AdminPollPageModel
{
    public PollFormViewModel? Form { get; private set; }

    public HomePollAdminDetail? Poll { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var poll = await homePollRepository.GetByIdAsync(id, cancellationToken);
        if (poll is null)
        {
            return NotFound();
        }

        Poll = poll;
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Edit poll";
        Form = BuildForm(poll, ToDraft(poll), null);
        return Page();
    }

    public static PollFormViewModel BuildForm(
        HomePollAdminDetail poll,
        AdminHomePollDraft draft,
        IReadOnlyList<string>? errors) =>
        new(
            "Edit poll",
            $"/admin/polls/{poll.Id}",
            draft,
            errors,
            OptionsLocked: poll.VoteCount > 0);

    public static AdminHomePollDraft ToDraft(HomePollAdminDetail poll) =>
        new(poll.Question, poll.Options.Select(option => option.OptionText).ToList());
}
