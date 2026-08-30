using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Polls;

public sealed record PollFormViewModel(
    string Title,
    string Action,
    AdminHomePollDraft Draft,
    IReadOnlyList<string>? Errors,
    bool OptionsLocked = false);
