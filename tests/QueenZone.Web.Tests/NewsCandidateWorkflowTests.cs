using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsCandidateWorkflowTests
{
    [Theory]
    [InlineData(NewsCandidateStatus.Discovered, NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.Discovered, NewsCandidateStatus.PromotedToArticle, false)]
    [InlineData(NewsCandidateStatus.NeedsReview, NewsCandidateStatus.Drafted, true)]
    [InlineData(NewsCandidateStatus.Drafted, NewsCandidateStatus.PromotedToArticle, true)]
    [InlineData(NewsCandidateStatus.Rejected, NewsCandidateStatus.NeedsReview, false)]
    [InlineData(NewsCandidateStatus.PromotedToArticle, NewsCandidateStatus.Drafted, false)]
    public void TryTransition_enforces_allowed_status_changes(
        NewsCandidateStatus current,
        NewsCandidateStatus next,
        bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.TryTransition(current, next, out _));
}
