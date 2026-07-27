namespace QueenZone.Web.Tests;

public sealed class FreddieTributeRoutesTests
{
    [Fact]
    public void GetIndexPath_ReturnsTributeRoot() =>
        Assert.Equal("/freddie-mercury-tribute", FreddieTributeRoutes.GetIndexPath());

    [Fact]
    public void GetPagePath_UsesIndexPathForFirstPage() =>
        Assert.Equal("/freddie-mercury-tribute", FreddieTributeRoutes.GetPagePath(1));

    [Fact]
    public void GetPagePath_UsesPageSegmentAfterFirstPage() =>
        Assert.Equal("/freddie-mercury-tribute/page/3", FreddieTributeRoutes.GetPagePath(3));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(12, 1)]
    [InlineData(13, 2)]
    [InlineData(49, 5)]
    public void GetTotalPages_UsesTwelveItemPageSize(int visibleCount, int expectedPages) =>
        Assert.Equal(expectedPages, FreddieTributeRoutes.GetTotalPages(visibleCount));
}

