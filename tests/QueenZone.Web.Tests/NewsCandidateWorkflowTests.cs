using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsCandidateWorkflowTests
{
    public static TheoryData<NewsCandidateStatus, NewsCandidateStatus, bool> TransitionMatrix
    {
        get
        {
            var data = new TheoryData<NewsCandidateStatus, NewsCandidateStatus, bool>();
            foreach (NewsCandidateStatus current in Enum.GetValues<NewsCandidateStatus>())
            {
                foreach (NewsCandidateStatus next in Enum.GetValues<NewsCandidateStatus>())
                {
                    var expected = current == next || ExpectedAllowed(current, next);
                    data.Add(current, next, expected);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(TransitionMatrix))]
    public void TryTransition_matches_full_status_matrix(
        NewsCandidateStatus current,
        NewsCandidateStatus next,
        bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.TryTransition(current, next, out _));

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
        Assert.Contains("Drafted", invalidError, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NewsCandidateStatus.Discovered, NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.PromotedToArticle, NewsCandidateStatus.Drafted, false)]
    public void CanTransition_matches_try_transition(
        NewsCandidateStatus current,
        NewsCandidateStatus next,
        bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData(NewsCandidateStatus.Rejected)]
    [InlineData(NewsCandidateStatus.IgnoredDuplicate)]
    [InlineData(NewsCandidateStatus.PromotedToArticle)]
    public void IsTerminal_is_true_for_closed_statuses(NewsCandidateStatus status) =>
        Assert.True(NewsCandidateWorkflow.IsTerminal(status));

    [Theory]
    [InlineData(NewsCandidateStatus.Discovered)]
    [InlineData(NewsCandidateStatus.NeedsReview)]
    [InlineData(NewsCandidateStatus.Drafted)]
    public void IsTerminal_is_false_for_open_statuses(NewsCandidateStatus status) =>
        Assert.False(NewsCandidateWorkflow.IsTerminal(status));

    [Fact]
    public void TryValidateStatusChange_rejects_already_at_target_with_clear_message()
    {
        Assert.False(NewsCandidateWorkflow.TryValidateStatusChange(
            NewsCandidateStatus.Rejected,
            NewsCandidateStatus.Rejected,
            out var error));
        Assert.Equal("This candidate has already been rejected.", error);

        Assert.False(NewsCandidateWorkflow.TryValidateStatusChange(
            NewsCandidateStatus.IgnoredDuplicate,
            NewsCandidateStatus.IgnoredDuplicate,
            out error));
        Assert.Equal("This candidate has already been ignored as a duplicate.", error);
    }

    [Fact]
    public void TryValidateStatusChange_allows_valid_change_and_rejects_invalid()
    {
        Assert.True(NewsCandidateWorkflow.TryValidateStatusChange(
            NewsCandidateStatus.Discovered,
            NewsCandidateStatus.NeedsReview,
            out var okError));
        Assert.Null(okError);

        Assert.False(NewsCandidateWorkflow.TryValidateStatusChange(
            NewsCandidateStatus.Discovered,
            NewsCandidateStatus.PromotedToArticle,
            out var badError));
        Assert.Equal(
            NewsCandidateWorkflow.FormatInvalidTransitionError(
                NewsCandidateStatus.Discovered,
                NewsCandidateStatus.PromotedToArticle),
            badError);
    }

    [Theory]
    [InlineData(NewsCandidateStatus.Discovered, true)]
    [InlineData(NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.Drafted, true)]
    [InlineData(NewsCandidateStatus.Rejected, false)]
    [InlineData(NewsCandidateStatus.IgnoredDuplicate, false)]
    [InlineData(NewsCandidateStatus.PromotedToArticle, false)]
    public void CanReject_matches_transition_rules(NewsCandidateStatus status, bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanReject(status));

    [Theory]
    [InlineData(NewsCandidateStatus.Discovered, true)]
    [InlineData(NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.Drafted, true)]
    [InlineData(NewsCandidateStatus.Rejected, false)]
    public void CanIgnoreAsDuplicate_matches_transition_rules(NewsCandidateStatus status, bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanIgnoreAsDuplicate(status));

    [Theory]
    [InlineData(NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.Discovered, false)]
    [InlineData(NewsCandidateStatus.Drafted, false)]
    public void CanMarkDrafted_requires_needs_review(NewsCandidateStatus status, bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanMarkDrafted(status));

    [Theory]
    [InlineData(NewsCandidateStatus.Drafted, true)]
    [InlineData(NewsCandidateStatus.NeedsReview, false)]
    [InlineData(NewsCandidateStatus.Discovered, false)]
    public void CanPromoteToArticle_requires_drafted(NewsCandidateStatus status, bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanPromoteToArticle(status));

    [Theory]
    [InlineData(NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.Drafted, true)]
    [InlineData(NewsCandidateStatus.Discovered, false)]
    [InlineData(NewsCandidateStatus.Rejected, false)]
    public void CanGenerateOrRegenerateDraft_allows_review_and_drafted(
        NewsCandidateStatus status,
        bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanGenerateOrRegenerateDraft(status));

    [Theory]
    [InlineData(NewsCandidateStatus.Discovered, true)]
    [InlineData(NewsCandidateStatus.NeedsReview, false)]
    [InlineData(NewsCandidateStatus.Drafted, false)]
    public void CanTriage_requires_discovered(NewsCandidateStatus status, bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanTriage(status));

    [Theory]
    [InlineData(NewsCandidateStatus.Drafted, true)]
    [InlineData(NewsCandidateStatus.NeedsReview, true)]
    [InlineData(NewsCandidateStatus.Discovered, false)]
    [InlineData(NewsCandidateStatus.PromotedToArticle, false)]
    public void CanPrepareForPromotion_allows_drafted_and_needs_review(
        NewsCandidateStatus status,
        bool expected) =>
        Assert.Equal(expected, NewsCandidateWorkflow.CanPrepareForPromotion(status));

    [Fact]
    public void Capability_error_helpers_return_clear_messages()
    {
        Assert.Contains(
            "discovered",
            NewsCandidateWorkflow.GetTriageError(NewsCandidateStatus.NeedsReview),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "Only needs-review or drafted candidates can regenerate a draft",
            NewsCandidateWorkflow.GetDraftGenerationError(NewsCandidateStatus.Discovered),
            StringComparison.Ordinal);
        Assert.Contains(
            "Only drafted candidates can be promoted",
            NewsCandidateWorkflow.GetPromoteReadinessError(NewsCandidateStatus.Discovered),
            StringComparison.Ordinal);
        Assert.Equal(string.Empty, NewsCandidateWorkflow.GetPromoteReadinessError(NewsCandidateStatus.NeedsReview));
        Assert.Equal(string.Empty, NewsCandidateWorkflow.GetPromoteReadinessError(NewsCandidateStatus.Drafted));
        Assert.Equal(string.Empty, NewsCandidateWorkflow.GetDraftGenerationError(NewsCandidateStatus.Drafted));
        Assert.Equal(string.Empty, NewsCandidateWorkflow.GetTriageError(NewsCandidateStatus.Discovered));
    }

    [Fact]
    public void GetAllowedTransitions_returns_configured_targets()
    {
        Assert.Equal(
            [
                NewsCandidateStatus.NeedsReview,
                NewsCandidateStatus.Rejected,
                NewsCandidateStatus.IgnoredDuplicate
            ],
            NewsCandidateWorkflow.GetAllowedTransitions(NewsCandidateStatus.Discovered));
        Assert.Empty(NewsCandidateWorkflow.GetAllowedTransitions(NewsCandidateStatus.Rejected));
    }

    private static bool ExpectedAllowed(NewsCandidateStatus current, NewsCandidateStatus next) =>
        (current, next) switch
        {
            (NewsCandidateStatus.Discovered, NewsCandidateStatus.NeedsReview) => true,
            (NewsCandidateStatus.Discovered, NewsCandidateStatus.Rejected) => true,
            (NewsCandidateStatus.Discovered, NewsCandidateStatus.IgnoredDuplicate) => true,
            (NewsCandidateStatus.NeedsReview, NewsCandidateStatus.Drafted) => true,
            (NewsCandidateStatus.NeedsReview, NewsCandidateStatus.Rejected) => true,
            (NewsCandidateStatus.NeedsReview, NewsCandidateStatus.IgnoredDuplicate) => true,
            (NewsCandidateStatus.Drafted, NewsCandidateStatus.PromotedToArticle) => true,
            (NewsCandidateStatus.Drafted, NewsCandidateStatus.NeedsReview) => true,
            (NewsCandidateStatus.Drafted, NewsCandidateStatus.Rejected) => true,
            (NewsCandidateStatus.Drafted, NewsCandidateStatus.IgnoredDuplicate) => true,
            _ => false
        };
}
