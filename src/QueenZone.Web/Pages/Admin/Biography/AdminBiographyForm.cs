using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Biography;

public sealed class AdminBiographyForm
{
    [FromForm(Name = "title")]
    public string Title { get; init; } = string.Empty;

    [FromForm(Name = "summary")]
    public string Summary { get; init; } = string.Empty;

    [FromForm(Name = "body")]
    public string Body { get; init; } = string.Empty;

    [FromForm(Name = "displaySequence")]
    public byte DisplaySequence { get; init; }

    public AdminBiographyDraft ToDraft() =>
        new(
            (Title ?? string.Empty).Trim(),
            (Summary ?? string.Empty).Trim(),
            Body ?? string.Empty,
            DisplaySequence);
}
