using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed class AdminQuoteForm
{
    [FromForm(Name = "text")]
    public string Text { get; init; } = string.Empty;

    [FromForm(Name = "whoSaid")]
    public string WhoSaid { get; init; } = string.Empty;

    [FromForm(Name = "isPublished")]
    public bool IsPublished { get; init; }

    [FromForm(Name = "context")]
    public string? Context { get; init; }

    public AdminQuoteDraft ToDraft() =>
        new(
            (Text ?? string.Empty).Trim(),
            (WhoSaid ?? string.Empty).Trim(),
            IsPublished,
            string.IsNullOrWhiteSpace(Context) ? null : Context.Trim());
}
