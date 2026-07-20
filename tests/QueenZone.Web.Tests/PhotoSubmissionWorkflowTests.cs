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
}
