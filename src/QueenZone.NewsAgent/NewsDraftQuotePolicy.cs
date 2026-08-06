using System.Text;
using System.Text.RegularExpressions;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

/// <summary>
/// Enforces draft quote safety: only evidence-backed quotes may remain in quotation marks.
/// </summary>
public static partial class NewsDraftQuotePolicy
{
    public static NewsDraftStructuredResult Enforce(
        NewsDraftStructuredResult draft,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(evidence);

        var evidenceText = BuildEvidenceText(evidence);
        var verifiedQuotes = draft.PreservedQuotes
            .Where(quote => IsPresentInEvidence(quote.ExactText, evidenceText))
            .Select(quote => quote with
            {
                ExactText = quote.ExactText.Trim(),
                Speaker = string.IsNullOrWhiteSpace(quote.Speaker) ? "Unknown" : quote.Speaker.Trim(),
                SourceUrl = string.IsNullOrWhiteSpace(quote.SourceUrl)
                    ? InferSourceUrl(quote.ExactText, evidence)
                    : quote.SourceUrl.Trim()
            })
            .ToList();

        var verifiedExactTexts = verifiedQuotes
            .Select(quote => quote.ExactText)
            .ToHashSet(StringComparer.Ordinal);

        var sanitizedBody = QuotedPassageRegex().Replace(draft.Body, match =>
        {
            var quoted = match.Groups["text"].Value;
            if (verifiedExactTexts.Contains(quoted.Trim()))
            {
                return match.Value;
            }

            // Unverifiable quote: drop the marks and keep the wording as paraphrase.
            return quoted;
        });

        return draft with
        {
            Body = sanitizedBody.Trim(),
            PreservedQuotes = verifiedQuotes
        };
    }

    private static string BuildEvidenceText(IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var builder = new StringBuilder();
        foreach (var item in evidence)
        {
            builder.AppendLine(item.FetchedTitle);
            builder.AppendLine(item.Excerpt);
            builder.AppendLine(item.SourceUrl);
            builder.AppendLine(item.CanonicalUrl);
        }

        return builder.ToString();
    }

    private static bool IsPresentInEvidence(string exactText, string evidenceText)
    {
        if (string.IsNullOrWhiteSpace(exactText))
        {
            return false;
        }

        return evidenceText.Contains(exactText.Trim(), StringComparison.Ordinal);
    }

    private static string InferSourceUrl(string exactText, IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        foreach (var item in evidence)
        {
            var haystack = $"{item.FetchedTitle}\n{item.Excerpt}";
            if (haystack.Contains(exactText, StringComparison.Ordinal))
            {
                return item.CanonicalUrl;
            }
        }

        return evidence.FirstOrDefault()?.CanonicalUrl ?? string.Empty;
    }

    [GeneratedRegex("[\"“](?<text>[^\"”]+)[\"”]", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedPassageRegex();
}
