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

    [Fact]
    public void Build_preserves_discovery_source_url_up_to_validation_limit()
    {
        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var longUrl = "https://www.queenonline.com/news/" + new string('a', 150);
        var candidate = new NewsCandidate(
            1,
            2,
            longUrl,
            longUrl,
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
            null,
            null,
            null,
            discoveredAt.Date,
            discoveredAt,
            discoveredAt);

        var adminDraft = NewsDiscoveryPromoteDraft.Build(agentDraft, candidate);
        var errors = NewsValidation.ValidateDraft(adminDraft, slugInUse: false);

        Assert.Equal(longUrl, adminDraft.SourceUrl);
        Assert.Empty(errors);
    }
}
