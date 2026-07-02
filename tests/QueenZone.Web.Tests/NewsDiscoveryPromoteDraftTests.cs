using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryPromoteDraftTests
{
    [Fact]
    public void Build_appends_attribution_to_body()
    {
        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidate = new NewsCandidate(
            1,
            2,
            "https://www.queenonline.com/news/example",
            "https://www.queenonline.com/news/example",
            "hash",
            "Source title",
            discoveredAt,
            discoveredAt,
            null,
            NewsCandidateStatus.Drafted,
            0.9m,
            0.8m,
            null,
            null,
            null,
            discoveredAt,
            discoveredAt,
            "queen-online",
            "Queen Online",
            NewsDiscoveryTrustTier.Primary);
        var agentDraft = new NewsAgentDraft(
            10,
            1,
            null,
            "Draft title",
            "draft-title",
            "Draft excerpt",
            "Draft body",
            "Source: Queen Online",
            "Summary",
            "Notes",
            discoveredAt.Date,
            discoveredAt,
            discoveredAt);

        var adminDraft = NewsDiscoveryPromoteDraft.Build(agentDraft, candidate);

        Assert.Equal("Draft body\n\nSource: Queen Online", adminDraft.Body);
        Assert.Equal(candidate.SourceUrl, adminDraft.SourceUrl);
    }
}
