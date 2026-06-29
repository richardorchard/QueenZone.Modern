namespace QueenZone.Web.Tests;

public sealed class FanPerformanceRoutesTests
{
    [Fact]
    public void GetIndexPath_ReturnsFanPerformancesRoot() =>
        Assert.Equal("/fan-performances", FanPerformanceRoutes.GetIndexPath());

    [Fact]
    public void GetPagePath_UsesIndexPathForFirstPage() =>
        Assert.Equal("/fan-performances", FanPerformanceRoutes.GetPagePath(1));

    [Fact]
    public void GetPagePath_UsesPageSegmentAfterFirstPage() =>
        Assert.Equal("/fan-performances/page/3", FanPerformanceRoutes.GetPagePath(3));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(149, 8)]
    public void GetTotalPages_UsesTwentyItemPageSize(int visibleCount, int expectedPages) =>
        Assert.Equal(expectedPages, FanPerformanceRoutes.GetTotalPages(visibleCount));
}
