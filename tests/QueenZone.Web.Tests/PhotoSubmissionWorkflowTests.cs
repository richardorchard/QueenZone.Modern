using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PhotoSubmissionWorkflowTests
{
    [Fact]
    public async Task UpdateStatusAsync_validates_allowed_transitions()
    {
        var repository = new InMemoryPhotoSubmissionRepository();
        var created = await repository.CreateAsync(
            new NewPhotoSubmission(
                Guid.NewGuid(),
                "Test photo",
                null,
                "Queen",
                1986,
                null,
                "members/test/original.jpg",
                "members/test/display.webp",
                "members/test/thumb.webp",
                "test.jpg",
                1024,
                "image/jpeg",
                800,
                600));

        var underReview = await repository.UpdateStatusAsync(
            created.Id,
            PhotoSubmissionStatus.UnderReview,
            "admin@test.local",
            "Starting review",
            null);
        Assert.NotNull(underReview);
        Assert.Equal(PhotoSubmissionStatus.UnderReview, underReview!.Status);

        var approved = await repository.UpdateStatusAsync(
            created.Id,
            PhotoSubmissionStatus.Approved,
            "admin@test.local",
            "Looks good",
            null,
            "Queen");
        Assert.Equal(PhotoSubmissionStatus.Approved, approved!.Status);
        Assert.Equal("Queen", approved.ApprovedCategory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                PhotoSubmissionStatus.Rejected,
                "admin@test.local",
                null,
                "Too late"));
        Assert.Contains("Cannot transition", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_reject_requires_reason()
    {
        var repository = new InMemoryPhotoSubmissionRepository();
        var created = await repository.CreateAsync(
            new NewPhotoSubmission(
                Guid.NewGuid(),
                "Reject me",
                null,
                null,
                null,
                null,
                "members/test/original.jpg",
                "members/test/display.webp",
                "members/test/thumb.webp",
                "test.jpg",
                1024,
                "image/jpeg",
                100,
                100));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                PhotoSubmissionStatus.Rejected,
                "admin@test.local",
                null,
                rejectionReason: null));
    }

    [Fact]
    public void Workflow_helpers_cover_terminal_and_unknown_statuses()
    {
        Assert.True(PhotoSubmissionWorkflow.IsTerminal(PhotoSubmissionStatus.Approved));
        Assert.True(PhotoSubmissionWorkflow.IsTerminal(PhotoSubmissionStatus.Rejected));
        Assert.False(PhotoSubmissionWorkflow.IsTerminal(PhotoSubmissionStatus.Pending));

        Assert.False(PhotoSubmissionWorkflow.CanTransition("Nope", PhotoSubmissionStatus.Approved));
        Assert.False(PhotoSubmissionWorkflow.TryValidateStatusChange("Nope", PhotoSubmissionStatus.Approved, out var unknownCurrent));
        Assert.Contains("Unknown current", unknownCurrent);

        Assert.False(PhotoSubmissionWorkflow.TryValidateStatusChange(PhotoSubmissionStatus.Pending, "Nope", out var unknownNext));
        Assert.Contains("Unknown target", unknownNext);

        Assert.False(PhotoSubmissionWorkflow.TryValidateStatusChange(
            PhotoSubmissionStatus.Pending,
            PhotoSubmissionStatus.Pending,
            out var same));
        Assert.Contains("already Pending", same);
    }

    [Fact]
    public async Task InMemory_GetPendingAndMissingPaths()
    {
        var repository = new InMemoryPhotoSubmissionRepository();
        var created = await repository.CreateAsync(
            new NewPhotoSubmission(
                Guid.NewGuid(),
                "Queue item",
                null,
                null,
                null,
                null,
                "members/test/original.jpg",
                "members/test/display.webp",
                "members/test/thumb.webp",
                "test.jpg",
                1024,
                "image/jpeg",
                100,
                100));

        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
        var pending = await repository.GetPendingAsync(0, 1000);
        Assert.Contains(pending, item => item.Id == created.Id);
        Assert.Equal("Unknown member", pending[0].SubmitterDisplayName);

        var bySubmitter = await repository.GetBySubmitterAsync(created.SubmitterMemberId);
        Assert.Single(bySubmitter);

        await repository.UpdateStatusAsync(
            created.Id,
            PhotoSubmissionStatus.NeedsInfo,
            "admin@test.local",
            "Need year",
            null);
        Assert.Contains(
            repository.GetAuditLogs(created.Id),
            log => log.Action == PhotoSubmissionStatus.NeedsInfo);
    }
}
