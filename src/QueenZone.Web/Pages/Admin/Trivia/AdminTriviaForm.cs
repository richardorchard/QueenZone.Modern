using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Trivia;

public sealed class AdminTriviaForm
{
    [FromForm(Name = "text")]
    public string Text { get; init; } = string.Empty;

    [FromForm(Name = "category")]
    public string? Category { get; init; }

    [FromForm(Name = "difficulty")]
    public string? Difficulty { get; init; }

    [FromForm(Name = "source")]
    public string? Source { get; init; }

    [FromForm(Name = "isPublished")]
    public bool IsPublished { get; init; }

    public AdminTriviaDraft ToDraft() =>
        new(
            (Text ?? string.Empty).Trim(),
            IsPublished,
            NormalizeOptional(Category),
            NormalizeDifficulty(Difficulty),
            NormalizeOptional(Source));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeDifficulty(string? value)
    {
        var trimmed = NormalizeOptional(value);
        return trimmed?.ToLowerInvariant();
    }
}
