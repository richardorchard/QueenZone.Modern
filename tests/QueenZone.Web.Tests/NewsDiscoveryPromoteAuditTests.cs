using System.Globalization;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryPromoteAuditTests
{
    [Fact]
    public void Format_includes_candidate_source_scores_and_rationale()
    {
        var provenance = new NewsDiscoveryProvenance(
            CandidateId: 42,
            SourceTitle: "Queen tour announced",
            SourceUrl: "https://www.queenonline.com/news/tour",
            SourceDisplayName: "Queen Online",
            SourceTrustTier: NewsDiscoveryTrustTier.Primary,
            RelevanceScore: 0.91m,
            ConfidenceScore: 0.88m,
            TriageRationale: "Official tour announcement.",
            DraftModelId: "openai/gpt-4.1-mini",
            DiscoveredAtUtc: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            SuggestedPublishAt: new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));

        var details = NewsDiscoveryPromoteAudit.Format(provenance);

        Assert.Contains("candidate #42", details, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queen Online", details, StringComparison.Ordinal);
        Assert.Contains("https://www.queenonline.com/news/tour", details, StringComparison.Ordinal);
        Assert.Contains("relevance 0.91", details, StringComparison.Ordinal);
        Assert.Contains("confidence 0.88", details, StringComparison.Ordinal);
        Assert.Contains("Official tour announcement", details, StringComparison.Ordinal);
        Assert.Contains("openai/gpt-4.1-mini", details, StringComparison.Ordinal);
        Assert.Contains("2026-06-02", details, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_truncates_to_audit_column_limit()
    {
        var provenance = new NewsDiscoveryProvenance(
            CandidateId: 1,
            SourceTitle: "Title",
            SourceUrl: "https://example.com",
            SourceDisplayName: "Example",
            SourceTrustTier: NewsDiscoveryTrustTier.Secondary,
            RelevanceScore: null,
            ConfidenceScore: null,
            TriageRationale: new string('x', 3000),
            DraftModelId: null,
            DiscoveredAtUtc: DateTime.UtcNow,
            SuggestedPublishAt: null);

        var details = NewsDiscoveryPromoteAudit.Format(provenance);

        Assert.True(details.Length <= 2000, details.Length.ToString(CultureInfo.InvariantCulture));
        Assert.EndsWith("...", details, StringComparison.Ordinal);
    }
}
