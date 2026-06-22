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
}