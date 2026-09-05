using QueenZone.Routing;

namespace QueenZone.Search.Shared.Tests;

public sealed class FanPerformanceRoutesTests
{
    [Fact]
    public void GetIndexPath_ReturnsFanPerformancesRoot() =>
        Assert.Equal("/fan-performances", FanPerformanceRoutes.GetIndexPath());

    [Fact]
    public void GetPublicPath_UsesArchiveAnchorForPromotedStageId()
    {
        Assert.Equal("fan-performance-187", FanPerformanceRoutes.GetPublicItemAnchorId(187));
        Assert.Equal("/fan-performances#fan-performance-187", FanPerformanceRoutes.GetPublicPath(187));
    }

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

    [Fact]
    public void GetAudioPath_UsesPerformanceId() =>
        Assert.Equal("/fan-performances/187/audio", FanPerformanceRoutes.GetAudioPath(187));

    [Fact]
    public void GetApiPaths_UseContentV1Prefix()
    {
        Assert.Equal("/api/v1/content/fan-performances", FanPerformanceRoutes.GetApiListPath());
        Assert.Equal("/api/v1/content/fan-performances/187", FanPerformanceRoutes.GetApiDetailPath(187));
        Assert.Equal("/api/v1/content/fan-performances/187/audio", FanPerformanceRoutes.GetApiAudioPath(187));
        Assert.Equal("/fan-performances/187/report", FanPerformanceRoutes.GetReportPath(187));
        Assert.Equal("/api/v1/me/fan-performances/187/report", FanPerformanceRoutes.GetApiReportPath(187));
    }

    [Fact]
    public void GetDownloadFileName_SanitizesTitle() =>
        Assert.Equal("Reaching-Out.mp3", FanPerformanceRoutes.GetDownloadFileName("Reaching Out"));

    [Theory]
    [InlineData("Bohemian Rhapsody", "Bohemian-Rhapsody")]
    [InlineData("Don't Stop Me Now!", "Don-t-Stop-Me-Now")]
    [InlineData("(Live at Wembley)", "Live-at-Wembley")]
    public void GetAudioPath_WithTitle_SanitizesTitleAsFilename(string title, string expectedFilename) =>
        Assert.Equal(
            $"/fan-performances/187/audio/{expectedFilename}.mp3",
            FanPerformanceRoutes.GetAudioPath(187, title));

    [Fact]
    public void GetLoginPath_EncodesReturnUrl() =>
        Assert.Equal(
            "/account/login?returnUrl=%2Ffan-performances%2Fpage%2F2",
            FanPerformanceRoutes.GetLoginPath("/fan-performances/page/2"));
}
