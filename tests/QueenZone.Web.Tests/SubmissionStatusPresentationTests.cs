using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class SubmissionStatusPresentationTests
{
    [Theory]
    [InlineData(PhotoSubmissionStatus.Pending, "pending", "Pending")]
    [InlineData(PhotoSubmissionStatus.UnderReview, "review", "Under review")]
    [InlineData(PhotoSubmissionStatus.NeedsInfo, "attention", "Needs info")]
    [InlineData(PhotoSubmissionStatus.Approved, "success", "Approved")]
    [InlineData(PhotoSubmissionStatus.Rejected, "danger", "Rejected")]
    [InlineData(NewsSuggestionStatus.Promoted, "success", "Promoted")]
    [InlineData(NewsSuggestionStatus.Duplicate, "danger", "Duplicate")]
    [InlineData(ArticleSubmissionStatus.Draft, "pending", "Draft")]
    [InlineData(ArticleSubmissionStatus.RequiresRevision, "attention", "Requires revision")]
    [InlineData(ArticleSubmissionStatus.Published, "success", "Published")]
    public void GetModifierAndLabel_MapKnownStatuses(string status, string modifier, string label)
    {
        Assert.Equal(modifier, SubmissionStatusPresentation.GetModifier(status));
        Assert.Equal(label, SubmissionStatusPresentation.GetLabel(status));
    }

    [Fact]
    public void TruncateUrl_LeavesShortUrlsUnchanged()
    {
        Assert.Equal("https://example.com/a", SubmissionStatusPresentation.TruncateUrl("https://example.com/a"));
    }

    [Fact]
    public void TruncateUrl_TruncatesLongUrlsTo80Chars()
    {
        var url = "https://example.com/" + new string('x', 100);
        var truncated = SubmissionStatusPresentation.TruncateUrl(url);

        Assert.Equal(80, truncated.Length);
        Assert.EndsWith("…", truncated);
    }
}
