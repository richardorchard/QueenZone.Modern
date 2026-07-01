using QueenZone.Data;
using QueenZone.Web.Pages.Admin.NewsDiscovery;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryStatusLabelsTests
{
    [Theory]
    [InlineData(NewsCandidateStatus.NeedsReview, "Needs review")]
    [InlineData(NewsCandidateStatus.PromotedToArticle, "Promoted")]
    public void Format_maps_candidate_status_labels(NewsCandidateStatus status, string expected) =>
        Assert.Equal(expected, NewsDiscoveryStatusLabels.Format(status));

    [Theory]
    [InlineData(NewsDiscoveryTrustTier.Primary, "Primary")]
    [InlineData(NewsDiscoveryTrustTier.Secondary, "Secondary")]
    public void FormatTrustTier_maps_trust_tier_labels(NewsDiscoveryTrustTier trustTier, string expected) =>
        Assert.Equal(expected, NewsDiscoveryStatusLabels.FormatTrustTier(trustTier));
}
