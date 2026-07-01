using System.Globalization;
using System.Text;

namespace QueenZone.Data;

public static class NewsDiscoveryPromoteAudit
{
    public static string Format(NewsDiscoveryProvenance provenance)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"Promoted from discovery candidate #{provenance.CandidateId}.");
        builder.Append(CultureInfo.InvariantCulture, $" Source: {provenance.SourceDisplayName} ({provenance.SourceTrustTier}).");
        builder.Append(CultureInfo.InvariantCulture, $" URL: {provenance.SourceUrl}.");

        if (provenance.RelevanceScore is not null || provenance.ConfidenceScore is not null)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $" Scores: relevance {provenance.RelevanceScore?.ToString("0.00", CultureInfo.InvariantCulture) ?? "—"}, confidence {provenance.ConfidenceScore?.ToString("0.00", CultureInfo.InvariantCulture) ?? "—"}.");
        }

        if (!string.IsNullOrWhiteSpace(provenance.TriageRationale))
        {
            builder.Append(" AI rationale: ");
            builder.Append(Truncate(provenance.TriageRationale, 400));
            builder.Append('.');
        }

        if (!string.IsNullOrWhiteSpace(provenance.DraftModelId))
        {
            builder.Append(CultureInfo.InvariantCulture, $" Draft model: {provenance.DraftModelId}.");
        }

        if (provenance.SuggestedPublishAt is DateTime suggested)
        {
            builder.Append(CultureInfo.InvariantCulture, $" Suggested publish date: {suggested:yyyy-MM-dd}.");
        }

        return Truncate(builder.ToString(), 2000);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
}
