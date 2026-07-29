using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfLinksRepositorySqlTests
{
    [Fact]
    public void PublicLinksSql_DoesNotRequireLinkChecksTable()
    {
        Assert.Contains("IF OBJECT_ID(N'dbo.QueenLinkChecks', N'U') IS NULL", EfLinksRepository.PublicLinksSql);
        Assert.Contains("LEFT JOIN dbo.QueenLinkChecks", EfLinksRepository.PublicLinksSql);
    }

    [Fact]
    public void LinkSql_CastsLegacyFlagColumnsBeforeMapping()
    {
        Assert.Contains("CAST(ISNULL(s.FEATURED_SITE, 0) AS int) AS FeaturedSite", EfLinksRepository.PublicLinksSql);
        Assert.Contains("ISNULL(CAST(s.DISPLAY AS int), 0) <> 0", EfLinksRepository.PublicLinksSql);
        Assert.Contains("CAST(ISNULL(s.FEATURED_SITE, 0) AS int) AS FeaturedSite", EfLinksRepository.ValidationLinksSql);
        Assert.Contains("ISNULL(CAST(s.DISPLAY AS int), 0) <> 0", EfLinksRepository.ValidationLinksSql);
    }

    [Fact]
    public void LinkSql_CastsLegacyIdColumnsBeforeMapping()
    {
        Assert.Contains("CAST(c.Q_LINK_CAT_ID AS int) AS CategoryId", EfLinksRepository.PublicLinksSql);
        Assert.Contains("CAST(s.QUEEN_FEATURED_SITE_ID AS int) AS Id", EfLinksRepository.PublicLinksSql);
        Assert.Contains("CAST(s.Q_LINK_CAT_ID AS int) AS CategoryId", EfLinksRepository.ValidationLinksSql);
        Assert.Contains("CAST(s.QUEEN_FEATURED_SITE_ID AS int) AS Id", EfLinksRepository.ValidationLinksSql);
    }
}
