using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PrivateMessageReportStatusTests
{
    [Theory]
    [InlineData(PrivateMessageReportStatus.Open)]
    [InlineData(PrivateMessageReportStatus.Reviewed)]
    [InlineData(PrivateMessageReportStatus.Dismissed)]
    [InlineData(PrivateMessageReportStatus.Actioned)]
    public void Normalize_AcceptsKnownStatuses(string status)
    {
        Assert.Equal(status, PrivateMessageReportStatus.Normalize(status));
        Assert.True(PrivateMessageReportStatus.IsKnown(status));
    }

    [Fact]
    public void Normalize_RejectsUnknownStatus()
    {
        Assert.Throws<ArgumentException>(() => PrivateMessageReportStatus.Normalize("Pending"));
        Assert.False(PrivateMessageReportStatus.IsKnown("Pending"));
    }

    [Theory]
    [InlineData(PrivateMessageReportStatus.Open, "Open")]
    [InlineData(PrivateMessageReportStatus.Reviewed, "Reviewed")]
    [InlineData(PrivateMessageReportStatus.Dismissed, "Dismissed")]
    [InlineData(PrivateMessageReportStatus.Actioned, "Actioned")]
    public void DisplayName_ReturnsCanonicalLabel(string status, string expected)
    {
        Assert.Equal(expected, PrivateMessageReportStatus.DisplayName(status));
    }

    [Fact]
    public void ContextSerializer_RoundTripsAndToleratesBadJson()
    {
        var item = new PrivateMessageReportContextItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alice",
            "Hello",
            DateTimeOffset.Parse("2026-08-25T12:00:00Z"));
        var json = PrivateMessageReportContextSerializer.Serialize([item]);
        var roundTrip = Assert.Single(PrivateMessageReportContextSerializer.Deserialize(json));
        Assert.Equal(item.MessageId, roundTrip.MessageId);
        Assert.Equal("Hello", roundTrip.Body);
        Assert.Empty(PrivateMessageReportContextSerializer.Deserialize(null));
        Assert.Empty(PrivateMessageReportContextSerializer.Deserialize("{not-json"));
        Assert.Null(PrivateMessageReportContextSerializer.Serialize([]));
    }
}
