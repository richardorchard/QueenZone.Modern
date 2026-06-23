using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumRoutesTests
{
    [Fact]
    public void GetCategoryPath_BuildsSlugFromCategoryName()
    {
        var category = new ForumCategoryItem(
            1,
            "Queen - Serious Discussion",
            "SERIOUS discussion on the greatest band in the land",
            344_828,
            new DateTime(2017, 7, 15, 20, 30, 0, DateTimeKind.Utc),
            "In response to who likes Bowie",
            10);

        Assert.Equal("/forum/1/queen-serious-discussion", ForumRoutes.GetCategoryPath(category));
    }

    [Theory]
    [InlineData(500, "500")]
    [InlineData(1_500, "1.5k+")]
    [InlineData(1_250_000, "1.3M+")]
    public void FormatCount_UsesCompactSuffixesForLargeValues(long value, string expected) =>
        Assert.Equal(expected, ForumRoutes.FormatCount(value));

    [Fact]
    public void GetCategoryCanonicalPath_UsesPageSegmentAfterFirstPage()
    {
        var category = new ForumCategoryItem(1, "Queen - Serious Discussion", null, 0, null, null, 10);

        Assert.Equal("/forum/1/queen-serious-discussion", ForumRoutes.GetCategoryCanonicalPath(category));
        Assert.Equal("/forum/1/queen-serious-discussion/page/3", ForumRoutes.GetCategoryCanonicalPath(category, 3));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(13075, 523)]
    public void GetTopicsTotalPages_MatchesLegacyStoredProcedurePaging(int totalCount, int expectedPages) =>
        Assert.Equal(expectedPages, ForumRoutes.GetTopicsTotalPages(totalCount));
}