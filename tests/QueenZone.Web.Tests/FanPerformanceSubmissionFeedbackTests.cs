using QueenZone.Data;
using QueenZone.Routing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceSubmissionFeedbackTests
{
    [Fact]
    public void GetMemberFacingNotes_Rejected_ReturnsRejectionReasonOnly()
    {
        var item = Sample(
            FanPerformanceSubmissionStatus.Rejected,
            reviewNotes: "internal reject note",
            rejectionReason: "Not a Queen cover");

        Assert.Equal("Not a Queen cover", FanPerformanceSubmissionFeedback.GetMemberFacingNotes(item));
    }

    [Fact]
    public void GetMemberFacingNotes_NeedsInfo_ReturnsReviewNotesAsk()
    {
        var item = Sample(
            FanPerformanceSubmissionStatus.NeedsInfo,
            reviewNotes: "Please name the Queen song",
            rejectionReason: "should not appear");

        Assert.Equal("Please name the Queen song", FanPerformanceSubmissionFeedback.GetMemberFacingNotes(item));
    }

    [Fact]
    public void GetMemberFacingNotes_Approved_HidesReviewNotes()
    {
        var item = Sample(
            FanPerformanceSubmissionStatus.Approved,
            reviewNotes: "internal approve note",
            rejectionReason: null,
            promotedStageId: 187);

        Assert.Null(FanPerformanceSubmissionFeedback.GetMemberFacingNotes(item));
    }

    [Fact]
    public void GetPublishedPath_Approved_UsesPromotedStageId()
    {
        var item = Sample(
            FanPerformanceSubmissionStatus.Approved,
            reviewNotes: "internal approve note",
            rejectionReason: null,
            promotedStageId: 187);

        Assert.Equal(FanPerformanceRoutes.GetPublicPath(187), FanPerformanceSubmissionFeedback.GetPublishedPath(item));
    }

    [Fact]
    public void GetPublishedPath_ApprovedWithoutPromotedStageId_ReturnsNull()
    {
        var item = Sample(FanPerformanceSubmissionStatus.Approved, "notes", null);
        Assert.Null(FanPerformanceSubmissionFeedback.GetPublishedPath(item));
    }

    [Theory]
    [InlineData(FanPerformanceSubmissionStatus.Pending)]
    [InlineData(FanPerformanceSubmissionStatus.Rejected)]
    [InlineData(FanPerformanceSubmissionStatus.NeedsInfo)]
    [InlineData(FanPerformanceSubmissionStatus.Withdrawn)]
    public void GetPublishedPath_NonApproved_ReturnsNull(string status)
    {
        var item = Sample(status, "notes", "reason", promotedStageId: 187);
        Assert.Null(FanPerformanceSubmissionFeedback.GetPublishedPath(item));
    }

    private static FanPerformanceSubmission Sample(
        string status,
        string? reviewNotes,
        string? rejectionReason,
        int? promotedStageId = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cover",
            "Song",
            "Fan",
            null,
            "blob",
            "cover.mp3",
            1024,
            "audio/mpeg",
            60,
            status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "admin@test.local",
            reviewNotes,
            rejectionReason,
            DateTimeOffset.UtcNow,
            FanPerformanceSubmissionRights.DeclarationVersion,
            promotedStageId);
}
