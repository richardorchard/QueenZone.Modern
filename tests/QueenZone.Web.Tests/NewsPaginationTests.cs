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
    public void GetArchivePaginationViewModel_OmitsNavigationForSinglePageArchive()
    {
        Assert.Null(NewsRoutes.GetArchivePaginationViewModel(1, 1));
    }

    [Fact]
    public void GetArchivePaginationViewModel_IncludesPreviousNextAndPageLinks()
    {
        var nav = NewsRoutes.GetArchivePaginationViewModel(2, 4);

        Assert.NotNull(nav);
        Assert.Equal("News archive pagination", nav.AriaLabel);
        Assert.Equal(2, nav.CurrentPage);
        Assert.Equal(4, nav.TotalPages);
        Assert.Equal("/news", nav.PreviousHref);
        Assert.Equal("/news/page/3", nav.NextHref);
        Assert.Contains(nav.Pages, p => p.PageNumber == 2 && p.IsCurrent && p.Href is null);
        Assert.Contains(nav.Pages, p => p.PageNumber == 3 && !p.IsCurrent && p.Href == "/news/page/3");
    }

    [Fact]
    public void GetArchivePaginationViewModel_ShowsDisabledPreviousOnFirstPage()
    {
        var nav = NewsRoutes.GetArchivePaginationViewModel(1, 3);

        Assert.NotNull(nav);
        Assert.Null(nav.PreviousHref);
        Assert.Equal("/news/page/2", nav.NextHref);
        Assert.Contains(nav.Pages, p => p.PageNumber == 2 && p.Href == "/news/page/2");
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
