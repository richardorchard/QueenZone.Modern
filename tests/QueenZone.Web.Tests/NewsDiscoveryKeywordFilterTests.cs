using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryKeywordFilterTests
{
    [Fact]
    public void Matches_allows_primary_sources_without_keyword_checks()
    {
        var source = CreateSource(NewsDiscoveryTrustTier.Primary, null);
        var item = new FetchedNewsItem("https://example.com/story", "Unrelated headline", null, null);

        Assert.True(NewsDiscoveryKeywordFilter.Matches(source, item));
    }

    [Fact]
    public void Matches_allows_secondary_sources_without_configured_keywords()
    {
        var source = CreateSource(NewsDiscoveryTrustTier.Secondary, null);
        var item = new FetchedNewsItem("https://example.com/story", "Unrelated headline", null, null);

        Assert.True(NewsDiscoveryKeywordFilter.Matches(source, item));
    }

    [Fact]
    public void Matches_requires_keyword_hit_for_secondary_sources()
    {
        var source = CreateSource(NewsDiscoveryTrustTier.Secondary, "queen,freddie mercury");
        var matching = new FetchedNewsItem("https://example.com/queen", "Queen tour", null, "Tour news");
        var nonMatching = new FetchedNewsItem("https://example.com/pedal", "Pedal review", null, "Gear");

        Assert.True(NewsDiscoveryKeywordFilter.Matches(source, matching));
        Assert.False(NewsDiscoveryKeywordFilter.Matches(source, nonMatching));
    }

    private static NewsDiscoverySource CreateSource(NewsDiscoveryTrustTier trustTier, string? keywords) =>
        new(
            1,
            "test-source",
            "Test Source",
            "https://example.com/",
            "https://example.com/feed",
            NewsDiscoverySourceType.Rss,
            trustTier,
            60,
            true,
            keywords,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);
}
