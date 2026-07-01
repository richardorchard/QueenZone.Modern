using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsTriageOptionsTests
{
    [Fact]
    public void Validate_rejects_invalid_score_ranges()
    {
        var options = new NewsTriageOptions { PrimaryMinRelevanceScore = 1.5m };

        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());

        Assert.Contains("PrimaryMinRelevanceScore", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MinRelevanceScore_uses_secondary_threshold_for_secondary_sources()
    {
        var options = new NewsTriageOptions { SecondaryMinRelevanceScore = 0.81m };

        Assert.Equal(0.81m, options.MinRelevanceScore(NewsDiscoveryTrustTier.Secondary));
    }
}
