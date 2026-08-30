using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class TriviaFactSubmissionWorkflowTests
{
    [Fact]
    public void Status_Normalize_accepts_known_values()
    {
        Assert.Equal(TriviaFactSubmissionStatus.Pending, TriviaFactSubmissionStatus.Normalize("pending"));
        Assert.Equal(TriviaFactSubmissionStatus.Approved, TriviaFactSubmissionStatus.Normalize("Approved"));
        Assert.Equal(TriviaFactSubmissionStatus.Rejected, TriviaFactSubmissionStatus.Normalize("REJECTED"));
    }

    [Fact]
    public void Status_Normalize_rejects_unknown_values()
    {
        Assert.Throws<ArgumentException>(() => TriviaFactSubmissionStatus.Normalize("UnderReview"));
    }

    [Fact]
    public void Status_IsKnown_and_IsPendingReview()
    {
        Assert.True(TriviaFactSubmissionStatus.IsKnown("Pending"));
        Assert.False(TriviaFactSubmissionStatus.IsKnown(" "));
        Assert.True(TriviaFactSubmissionStatus.IsPendingReview("pending"));
        Assert.False(TriviaFactSubmissionStatus.IsPendingReview("Approved"));
    }

    [Fact]
    public void Workflow_allows_pending_to_approve_or_reject()
    {
        Assert.True(TriviaFactSubmissionWorkflow.CanTransition(
            TriviaFactSubmissionStatus.Pending,
            TriviaFactSubmissionStatus.Approved));
        Assert.True(TriviaFactSubmissionWorkflow.CanTransition(
            TriviaFactSubmissionStatus.Pending,
            TriviaFactSubmissionStatus.Rejected));
        Assert.False(TriviaFactSubmissionWorkflow.CanTransition(
            TriviaFactSubmissionStatus.Approved,
            TriviaFactSubmissionStatus.Rejected));
        Assert.True(TriviaFactSubmissionWorkflow.IsTerminal(TriviaFactSubmissionStatus.Approved));
        Assert.True(TriviaFactSubmissionWorkflow.IsTerminal(TriviaFactSubmissionStatus.Rejected));
        Assert.False(TriviaFactSubmissionWorkflow.IsTerminal(TriviaFactSubmissionStatus.Pending));
    }

    [Fact]
    public void TryValidateStatusChange_reports_unknown_and_duplicate_and_illegal()
    {
        Assert.False(TriviaFactSubmissionWorkflow.TryValidateStatusChange("nope", "Pending", out var unknownCurrent));
        Assert.Contains("Unknown current status", unknownCurrent);

        Assert.False(TriviaFactSubmissionWorkflow.TryValidateStatusChange("Pending", "nope", out var unknownNext));
        Assert.Contains("Unknown target status", unknownNext);

        Assert.False(TriviaFactSubmissionWorkflow.TryValidateStatusChange("Pending", "pending", out var already));
        Assert.Contains("already Pending", already);

        Assert.False(TriviaFactSubmissionWorkflow.TryValidateStatusChange("Approved", "Rejected", out var illegal));
        Assert.Contains("Cannot transition", illegal);

        Assert.True(TriviaFactSubmissionWorkflow.TryValidateStatusChange("Pending", "Approved", out var ok));
        Assert.Null(ok);
    }

    [Fact]
    public void CanTransition_rejects_unknown_statuses()
    {
        Assert.False(TriviaFactSubmissionWorkflow.CanTransition("nope", TriviaFactSubmissionStatus.Approved));
        Assert.False(TriviaFactSubmissionWorkflow.CanTransition(TriviaFactSubmissionStatus.Pending, "nope"));
    }
}
