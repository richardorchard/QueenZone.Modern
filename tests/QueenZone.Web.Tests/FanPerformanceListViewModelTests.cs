namespace QueenZone.Web.Tests;

public sealed class FanPerformanceListViewModelTests
{
    [Fact]
    public void Empty_UsesIndexReturnPath()
    {
        var model = FanPerformanceListViewModel.Empty;

        Assert.Empty(model.Items);
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
}
