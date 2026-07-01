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

    [Fact]
    public void TryTransition_allows_same_status_and_returns_error_for_invalid_move()
    {
        Assert.True(NewsCandidateWorkflow.TryTransition(
            NewsCandidateStatus.Discovered,
            NewsCandidateStatus.Discovered,
            out var sameStatusError));
        Assert.Null(sameStatusError);

        Assert.False(NewsCandidateWorkflow.TryTransition(
            NewsCandidateStatus.Rejected,
            NewsCandidateStatus.Drafted,
            out var invalidError));
        Assert.Contains("Rejected", invalidError, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NewsCandidateStatus.Discovered, NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.PromotedToArticle, NewsCandidateStatus.Drafted, false)]
    public void CanTransition_matches_try_transition(
        NewsCandidateStatus current,
        NewsCandidateStatus next,
        bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanTransition(current, next));
}
