using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminNewsDiscoveryRoutesUnitTests
{
    [Fact]
    public void Normalize_clamps_page_size_and_normalizes_page_number()
    {
        var (page, pageSize) = NewsCandidateListQueryDefaults.Normalize(-2, 500);

        Assert.Equal(1, page);
        Assert.Equal(NewsCandidateListQueryDefaults.MaxPageSize, pageSize);
    }

    [Fact]
    public void BuildIndexPath_preserves_filters_and_page()
    {
        var path = AdminNewsDiscoveryRoutes.BuildIndexPath(
            new NewsDiscoveryIndexQuery(
                Status: NewsCandidateStatus.NeedsReview,
                SourceId: 4,
                TrustTier: NewsDiscoveryTrustTier.Primary,
                MinConfidence: 0.75m,
                Entity: "Brian May",
                DiscoveredFrom: new DateTime(2026, 7, 1),
                DiscoveredTo: new DateTime(2026, 7, 31),
                HasDraft: true,
                PageSize: 25),
            page: 2);

        Assert.Contains("page=2", path);
        Assert.Contains("status=NeedsReview", path);
        Assert.Contains("sourceId=4", path);
        Assert.Contains("trustTier=Primary", path);
        Assert.Contains("minConfidence=0.75", path);
        Assert.Contains("entity=Brian%20May", path);
        Assert.Contains("discoveredFrom=2026-07-01", path);
        Assert.Contains("discoveredTo=2026-07-31", path);
        Assert.Contains("hasDraft=true", path);
        Assert.Contains("pageSize=25", path);
    }
}
