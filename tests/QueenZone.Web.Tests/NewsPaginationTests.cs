using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsPaginationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(40, 2)]
    [InlineData(41, 3)]
    public void GetArchiveTotalPages_CalculatesExpectedPageCount(int publishedCount, int expectedTotalPages)
    {
        Assert.Equal(expectedTotalPages, NewsRoutes.GetArchiveTotalPages(publishedCount));
    }

    [Theory]
    [InlineData(1, "/news")]
    [InlineData(2, "/news/page/2")]
    [InlineData(12, "/news/page/12")]
    public void GetArchiveCanonicalPath_UsesNewsForFirstPage(int page, string expectedPath)
    {
        Assert.Equal(expectedPath, NewsRoutes.GetArchiveCanonicalPath(page));
    }

    [Theory]
    [InlineData(1, "QueenZone news")]
    [InlineData(2, "QueenZone news \u2013 Page 2")]
    public void GetArchivePageTitle_IncludesPageNumberAfterFirstPage(int page, string expectedTitle)
    {
        Assert.Equal(expectedTitle, NewsRoutes.GetArchivePageTitle(page));
    }

    [Fact]
    public void BuildArchivePaginationNav_OmitsNavigationForSinglePageArchive()
    {
        Assert.Equal(string.Empty, NewsRoutes.BuildArchivePaginationNav(1, 1));
    }

    [Fact]
    public void BuildArchivePaginationNav_IncludesPreviousNextAndPageLinks()
    {
        var nav = NewsRoutes.BuildArchivePaginationNav(2, 4);

        Assert.Contains("Page 2 of 4", nav);
        Assert.Contains("archive-pagination-controls", nav);
        Assert.Contains("archive-pagination-pages", nav);
        Assert.Contains("rel=\"prev\" href=\"/news\"", nav);
        Assert.Contains("href=\"/news/page/3\"", nav);
        Assert.Contains("rel=\"next\" href=\"/news/page/3\"", nav);
        Assert.Contains("aria-current=\"page\">2</span>", nav);
    }

    [Fact]
    public void BuildArchivePaginationNav_ShowsDisabledPreviousOnFirstPage()
    {
        var nav = NewsRoutes.BuildArchivePaginationNav(1, 3);

        Assert.Contains("archive-pagination-prev is-disabled", nav);
        Assert.Contains("rel=\"next\" href=\"/news/page/2\"", nav);
        Assert.Contains("href=\"/news/page/2\"", nav);
    }

    [Theory]
    [InlineData(1, 20, 0, 1, 2)]
    [InlineData(2, 5, 25, 2, 2)]
    [InlineData(1, 15, 15, 1, 1)]
    public void ResolveArchiveTotalPages_UsesFallbackWhenCountIsUnavailable(
        int currentPage,
        int itemCount,
        int publishedCount,
        int initialTotalPages,
        int expectedTotalPages)
    {
        Assert.Equal(
            expectedTotalPages,
            NewsRoutes.ResolveArchiveTotalPages(currentPage, itemCount, publishedCount, initialTotalPages));
    }
}
