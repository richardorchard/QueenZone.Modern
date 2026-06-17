using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class AdminNewsForm
{
    [FromForm(Name = "title")]
    public string Title { get; init; } = string.Empty;

    [FromForm(Name = "slug")]
    public string? Slug { get; init; }

    [FromForm(Name = "excerpt")]
    public string Excerpt { get; init; } = string.Empty;

    [FromForm(Name = "body")]
    public string Body { get; init; } = string.Empty;

    [FromForm(Name = "publishedAt")]
    public string PublishedAt { get; init; } = string.Empty;

    [FromForm(Name = "sourceUrl")]
    public string? SourceUrl { get; init; }

    public AdminNewsDraft ToDraft()
    {
        DateTime publishedAt = default;
        if (!string.IsNullOrWhiteSpace(PublishedAt)
            && DateTime.TryParse(PublishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            publishedAt = parsed;
        }

        return new AdminNewsDraft(
            (Title ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(Slug) ? null : Slug.Trim(),
            (Excerpt ?? string.Empty).Trim(),
            Body ?? string.Empty,
            publishedAt,
            string.IsNullOrWhiteSpace(SourceUrl) ? null : SourceUrl.Trim());
    }
}
