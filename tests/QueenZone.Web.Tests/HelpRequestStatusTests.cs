using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class HelpRequestStatusTests
{
    [Theory]
    [InlineData(HelpRequestStatus.Open, true)]
    [InlineData(HelpRequestStatus.InProgress, true)]
    [InlineData(HelpRequestStatus.Resolved, false)]
    [InlineData(HelpRequestStatus.Spam, false)]
    public void IsOpenQueue_MatchesActiveSupportStatuses(string status, bool expected)
    {
        Assert.Equal(expected, HelpRequestStatus.IsOpenQueue(status));
    }

    [Fact]
    public void Normalize_RejectsUnknownStatus()
    {
        Assert.Throws<ArgumentException>(() => HelpRequestStatus.Normalize("Pending"));
    }

    [Theory]
    [InlineData(HelpRequestTopic.Account, "Account")]
    [InlineData(HelpRequestTopic.Content, "Content on the site")]
    [InlineData("privacy", "Privacy / data")]
    public void TopicDisplayName_AcceptsKnownValues(string topic, string label)
    {
        Assert.Equal(label, HelpRequestTopic.DisplayName(topic));
    }
}
