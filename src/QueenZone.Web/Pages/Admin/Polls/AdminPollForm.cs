using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Polls;

public sealed class AdminPollForm
{
    [FromForm(Name = "question")]
    public string Question { get; init; } = string.Empty;

    [FromForm(Name = "optionTexts")]
    public List<string> OptionTexts { get; init; } = [];

    public AdminHomePollDraft ToDraft() =>
        new(
            (Question ?? string.Empty).Trim(),
            HomePollValidation.NormalizeOptions(OptionTexts));
}
