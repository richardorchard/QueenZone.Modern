using System.Net;

namespace QueenZone.NewsAgent;

public static class NewsDraftMediaLinkPolicy
{
    private static readonly string[] AllowedLabels = ["Listen to the song", "Watch the video"];

    public static NewsDraftStructuredResult Enforce(
        NewsDraftStructuredResult draft,
        IReadOnlyList<QueenZone.Data.NewsCandidateEvidence> evidence)
    {
        var missingLinks = evidence
            .SelectMany(item => ExtractMediaLinks(item.Excerpt))
            .DistinctBy(link => link.Url, StringComparer.OrdinalIgnoreCase)
            .Where(link => !ContainsUrl(draft.Body, link.Url))
            .ToList();

        if (missingLinks.Count == 0)
        {
            return draft;
        }

        var linkHtml = string.Join(
            Environment.NewLine,
            missingLinks.Select(link =>
                $"<p><a href=\"{WebUtility.HtmlEncode(link.Url)}\">{WebUtility.HtmlEncode(link.Label)}</a></p>"));

        return draft with
        {
            Body = draft.Body.TrimEnd() + Environment.NewLine + Environment.NewLine + linkHtml
        };
    }

    private static IEnumerable<(string Label, string Url)> ExtractMediaLinks(string? excerpt)
    {
        if (string.IsNullOrWhiteSpace(excerpt))
        {
            yield break;
        }

        foreach (var line in excerpt.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var label in AllowedLabels)
            {
                var prefix = $"- {label}: ";
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var url = line[prefix.Length..].Trim();
                if (OutboundUrlSafety.TryValidatePublicHttpUrl(url, out _, out var normalizedUrl)
                    && !string.IsNullOrWhiteSpace(normalizedUrl))
                {
                    yield return (label, normalizedUrl);
                }
            }
        }
    }

    private static bool ContainsUrl(string body, string url) =>
        body.Contains(url, StringComparison.OrdinalIgnoreCase)
        || body.Contains(WebUtility.HtmlEncode(url), StringComparison.OrdinalIgnoreCase);
}
