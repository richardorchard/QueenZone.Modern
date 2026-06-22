using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class StoriesPaginationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    public void GetArchiveTotalPages_CalculatesExpectedPageCount(int publishedCount, int expectedTotalPages)
    {
        Assert.Equal(expectedTotalPages, StoriesRoutes.GetArchiveTotalPages(publishedCount));
    }

    [Theory]
    [InlineData(1, "/stories")]
    [InlineData(2, "/stories/page/2")]
    public void GetArchiveCanonicalPath_UsesStoriesForFirstPage(int page, string expectedPath)
    {
        Assert.Equal(expectedPath, StoriesRoutes.GetArchiveCanonicalPath(page));
    }

    [Fact]
    public void BuildArchivePaginationNav_IncludesPreviousNextAndPageLinks()
    {
        var nav = StoriesRoutes.BuildArchivePaginationNav(2, 4);

        Assert.Contains("Page 2 of 4", nav);
        Assert.Contains("rel=\"prev\" href=\"/stories\"", nav);
        Assert.Contains("rel=\"next\" href=\"/stories/page/3\"", nav);
    }
}