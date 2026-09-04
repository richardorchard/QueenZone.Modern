using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceSubmissionWorkflowTests
{
    [Fact]
    public async Task UpdateStatusAsync_validates_allowed_transitions()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission());

        var underReview = await repository.UpdateStatusAsync(
            created.Id,
            FanPerformanceSubmissionStatus.UnderReview,
            "admin@test.local",
            "Starting review",
            null);
        Assert.NotNull(underReview);
        Assert.Equal(FanPerformanceSubmissionStatus.UnderReview, underReview!.Status);

        var approved = await repository.UpdateStatusAsync(
            created.Id,
            FanPerformanceSubmissionStatus.Approved,
            "admin@test.local",
            "Looks good",
            null);
        Assert.Equal(FanPerformanceSubmissionStatus.Approved, approved!.Status);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                FanPerformanceSubmissionStatus.Rejected,
                "admin@test.local",
                null,
                "Too late"));
        Assert.Contains("Cannot transition", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_allows_member_withdraw_from_reviewable_statuses()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var pending = await repository.CreateAsync(NewSubmission());
        var withdrawn = await repository.UpdateStatusAsync(
            pending.Id,
            FanPerformanceSubmissionStatus.Withdrawn,
            string.Empty,
            null,
            null,
            "Member withdrew the submission.");
        Assert.Equal(FanPerformanceSubmissionStatus.Withdrawn, withdrawn!.Status);
        Assert.True(FanPerformanceSubmissionWorkflow.IsTerminal(withdrawn.Status));
        Assert.Contains(
            repository.GetAuditLogs(pending.Id),
            log => log.Action == FanPerformanceSubmissionStatus.Withdrawn);

        var needsInfo = await repository.CreateAsync(NewSubmission());
        await repository.UpdateStatusAsync(
            needsInfo.Id,
            FanPerformanceSubmissionStatus.NeedsInfo,
            "admin@test.local",
            "Need a cleaner take",
            null);
        var replied = await repository.UpdateStatusAsync(
            needsInfo.Id,
            FanPerformanceSubmissionStatus.UnderReview,
            string.Empty,
            null,
            null,
            "Here is a cleaner take.");
        Assert.Equal(FanPerformanceSubmissionStatus.UnderReview, replied!.Status);
        Assert.Equal("Need a cleaner take", replied.ReviewNotes);
        Assert.Contains(
            repository.GetAuditLogs(needsInfo.Id),
            log => log.Details == "Here is a cleaner take.");
    }

    [Fact]
    public async Task UpdateStatusAsync_reject_requires_reason()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                FanPerformanceSubmissionStatus.Rejected,
                "admin@test.local",
                null,
                rejectionReason: null));
    }

    [Fact]
    public async Task UpdateStatusAsync_needs_info_requires_review_notes()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                FanPerformanceSubmissionStatus.NeedsInfo,
                "admin@test.local",
                reviewNotes: "   ",
                rejectionReason: null));
        Assert.Contains("Review notes are required", ex.Message);
    }

    [Fact]
    public void Workflow_helpers_cover_terminal_withdraw_and_unknown_statuses()
    {
        Assert.True(FanPerformanceSubmissionWorkflow.IsTerminal(FanPerformanceSubmissionStatus.Approved));
        Assert.True(FanPerformanceSubmissionWorkflow.IsTerminal(FanPerformanceSubmissionStatus.Rejected));
        Assert.True(FanPerformanceSubmissionWorkflow.IsTerminal(FanPerformanceSubmissionStatus.Withdrawn));
        Assert.False(FanPerformanceSubmissionWorkflow.IsTerminal(FanPerformanceSubmissionStatus.Pending));

        Assert.True(FanPerformanceSubmissionWorkflow.CanMemberWithdraw(FanPerformanceSubmissionStatus.Pending));
        Assert.True(FanPerformanceSubmissionWorkflow.CanMemberWithdraw(FanPerformanceSubmissionStatus.UnderReview));
        Assert.True(FanPerformanceSubmissionWorkflow.CanMemberWithdraw(FanPerformanceSubmissionStatus.NeedsInfo));
        Assert.False(FanPerformanceSubmissionWorkflow.CanMemberWithdraw(FanPerformanceSubmissionStatus.Approved));
        Assert.False(FanPerformanceSubmissionWorkflow.CanMemberWithdraw(FanPerformanceSubmissionStatus.Rejected));
        Assert.True(FanPerformanceSubmissionWorkflow.CanMemberReplyNeedsInfo(FanPerformanceSubmissionStatus.NeedsInfo));
        Assert.False(FanPerformanceSubmissionWorkflow.CanMemberReplyNeedsInfo(FanPerformanceSubmissionStatus.Pending));
        Assert.True(FanPerformanceSubmissionWorkflow.CanAdminAct(FanPerformanceSubmissionStatus.Pending));
        Assert.True(FanPerformanceSubmissionWorkflow.CanAdminAct(FanPerformanceSubmissionStatus.UnderReview));
        Assert.True(FanPerformanceSubmissionWorkflow.CanAdminAct(FanPerformanceSubmissionStatus.NeedsInfo));
        Assert.False(FanPerformanceSubmissionWorkflow.CanAdminAct(FanPerformanceSubmissionStatus.Approved));
        Assert.False(FanPerformanceSubmissionWorkflow.CanAdminAct(FanPerformanceSubmissionStatus.Rejected));
        Assert.False(FanPerformanceSubmissionWorkflow.CanAdminAct(FanPerformanceSubmissionStatus.Withdrawn));
        Assert.False(FanPerformanceSubmissionWorkflow.CanAdminAct("Nope"));

        Assert.False(FanPerformanceSubmissionWorkflow.CanTransition("Nope", FanPerformanceSubmissionStatus.Approved));
        Assert.False(FanPerformanceSubmissionWorkflow.TryValidateStatusChange("Nope", FanPerformanceSubmissionStatus.Approved, out var unknownCurrent));
        Assert.Contains("Unknown current", unknownCurrent);

        Assert.False(FanPerformanceSubmissionWorkflow.TryValidateStatusChange(FanPerformanceSubmissionStatus.Pending, "Nope", out var unknownNext));
        Assert.Contains("Unknown target", unknownNext);

        Assert.False(FanPerformanceSubmissionWorkflow.TryValidateStatusChange(
            FanPerformanceSubmissionStatus.Pending,
            FanPerformanceSubmissionStatus.Pending,
            out var same));
        Assert.Contains("already Pending", same);

        Assert.Equal(
            FanPerformanceSubmissionStatus.Pending,
            FanPerformanceSubmissionStatus.Normalize("pending"));
        Assert.Throws<ArgumentException>(() => FanPerformanceSubmissionStatus.Normalize("Nope"));
        Assert.False(FanPerformanceSubmissionWorkflow.CanMemberReplyNeedsInfo("Nope"));
        Assert.False(FanPerformanceSubmissionWorkflow.CanMemberWithdraw("Nope"));
    }

    [Fact]
    public async Task InMemory_GetBySubmitterAndMissingPaths()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission());

        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
        Assert.Null(await repository.UpdateStatusAsync(
            Guid.NewGuid(),
            FanPerformanceSubmissionStatus.UnderReview,
            "admin@test.local",
            null,
            null));
        var bySubmitter = await repository.GetBySubmitterAsync(created.SubmitterMemberId);
        Assert.Single(bySubmitter.Items);
        Assert.Equal(created.Id, bySubmitter.Items[0].Id);
    }

    private static NewFanPerformanceSubmission NewSubmission() =>
        new(
            Guid.NewGuid(),
            "Bohemian cover",
            "Bohemian Rhapsody",
            "A fan",
            "Living room take",
            "members/test/cover.mp3",
            "cover.mp3",
            1024,
            "audio/mpeg",
            180,
            DateTimeOffset.UtcNow,
            FanPerformanceSubmissionRights.DeclarationVersion);
}
