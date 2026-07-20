using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ArticleSubmissionStatusTests
{
    [Theory]
    [InlineData(ArticleSubmissionStatus.Published, true)]
    [InlineData(ArticleSubmissionStatus.Rejected, true)]
    [InlineData(ArticleSubmissionStatus.Draft, false)]
    [InlineData(ArticleSubmissionStatus.Submitted, false)]
    [InlineData(ArticleSubmissionStatus.UnderReview, false)]
    public void IsTerminal_RecognizesTerminalStatuses(string status, bool expected)
    {
        Assert.Equal(expected, ArticleSubmissionStatus.IsTerminal(status));
    }

    [Theory]
    [InlineData(ArticleSubmissionStatus.Submitted, true)]
    [InlineData(ArticleSubmissionStatus.UnderReview, true)]
    [InlineData(ArticleSubmissionStatus.ApprovedForPublishing, true)]
    [InlineData(ArticleSubmissionStatus.Draft, false)]
    [InlineData(ArticleSubmissionStatus.Published, false)]
    [InlineData(ArticleSubmissionStatus.Rejected, false)]
    [InlineData(ArticleSubmissionStatus.RequiresRevision, false)]
    public void CanBeActedOnByAdmin_RecognizesReviewableStatuses(string status, bool expected)
    {
        Assert.Equal(expected, ArticleSubmissionStatus.CanBeActedOnByAdmin(status));
    }
}
