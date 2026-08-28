using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceListViewModelTests
{
    [Fact]
    public void Empty_UsesIndexReturnPath()
    {
        var model = FanPerformanceListViewModel.Empty;

        Assert.Empty(model.Items);
        Assert.Empty(model.Catalog);
        Assert.False(model.CanPlayCatalog);
        Assert.Equal("[]", model.CatalogJson);
        Assert.Equal("/fan-performances", model.LoginReturnUrl);
    }

    [Fact]
    public void ListItem_ExposesPlaybackPathWhenProvided()
    {
        var added = new DateTime(2014, 4, 17, 15, 17, 0, DateTimeKind.Utc);
        var item = new FanPerformanceListItem(
            187,
            "Reaching Out",
            "Mike Ryde",
            "Cover recording.",
            added,
            "/fan-performances/187/audio");

        Assert.Equal(187, item.Id);
        Assert.Equal("/fan-performances/187/audio", item.AudioPlayPath);
    }

    [Fact]
    public void CreateCatalog_MapsIdsTitlesAndCookieGatedAudioPaths()
    {
        var catalog = FanPerformanceListViewModel.CreateCatalog(SampleFanPerformanceData.CreateSeedPerformances());

        Assert.Equal(4, catalog.Count);
        Assert.Equal(187, catalog[0].Id);
        Assert.Equal("Reaching Out", catalog[0].Title);
        Assert.Equal("/fan-performances/187/audio/Reaching-Out.mp3", catalog[0].AudioPlayPath);
        Assert.DoesNotContain(catalog, entry => entry.AudioPlayPath.Contains("songfiles", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CatalogJson_UsesCamelCaseAndEncodesHtml()
    {
        var added = new DateTime(2014, 4, 17, 15, 17, 0, DateTimeKind.Utc);
        var performances = new FanPerformance[]
        {
            new(12, "</script>Breakout", "Fan", "desc", "file.mp3", 10, added)
        };
        var model = new FanPerformanceListViewModel(
            [],
            "/fan-performances",
            FanPerformanceListViewModel.CreateCatalog(performances));

        Assert.True(model.CanPlayCatalog);
        Assert.Contains("\"id\":12", model.CatalogJson);
        Assert.Contains("\"title\":", model.CatalogJson);
        Assert.Contains("\"audioPlayPath\":", model.CatalogJson);
        Assert.DoesNotContain("</script>", model.CatalogJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C/script\\u003E", model.CatalogJson);
    }
}
