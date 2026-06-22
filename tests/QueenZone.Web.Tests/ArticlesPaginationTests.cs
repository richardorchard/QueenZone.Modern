using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ArticlesPaginationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    public void GetArchiveTotalPages_CalculatesExpectedPageCount(int publishedCount, int expectedTotalPages)
    {
        Assert.Equal(expectedTotalPages, ArticlesRoutes.GetArchiveTotalPages(publishedCount));
    }

    [Theory]
    [InlineData(1, "/articles")]
    [InlineData(2, "/articles/page/2")]
    public void GetArchiveCanonicalPath_UsesArticlesForFirstPage(int page, string expectedPath)
    {
        Assert.Equal(expectedPath, ArticlesRoutes.GetArchiveCanonicalPath(page));
    }

    [Fact]
    public void BuildArchivePaginationNav_IncludesPreviousNextAndPageLinks()
    {
        var nav = ArticlesRoutes.BuildArchivePaginationNav(2, 4);

        Assert.Contains("Page 2 of 4", nav);
        Assert.Contains("rel=\"prev\" href=\"/articles\"", nav);
        Assert.Contains("rel=\"next\" href=\"/articles/page/3\"", nav);
    }
}