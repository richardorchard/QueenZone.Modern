using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

public sealed class FanPerformanceSubmissionService(
    IFanPerformanceSubmissionRepository fanPerformanceSubmissionRepository,
    FanPerformanceAudioUploadService audioUploadService,
    IBlobUploadService blobUploadService)
{
    public const int MaxReplyLength = 500;

    public sealed record SubmitResult(bool Succeeded, FanPerformanceSubmission? Submission, string? Error);

    public sealed record ActionResult(bool Succeeded, FanPerformanceSubmission? Submission, string? Error);

    public async Task<SubmitResult> SubmitAsync(
        Guid memberAccountId,
        string title,
        string coveredSong,
        string performedBy,
        string? description,
        bool rightsDeclarationAccepted,
        Stream audioStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (memberAccountId == Guid.Empty)
        {
            return new SubmitResult(false, null, "Sign in is required to submit a fan performance.");
        }

        if (!rightsDeclarationAccepted)
        {
            return new SubmitResult(
                false,
                null,
                "You must confirm this recording is your own performance of a Queen song and agree to it being published on QueenZone.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return new SubmitResult(false, null, "Title is required.");
        }

        if (title.Trim().Length > 200)
        {
            return new SubmitResult(false, null, "Title must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(coveredSong))
        {
            return new SubmitResult(false, null, "Covered song is required.");
        }

        if (coveredSong.Trim().Length > 200)
        {
            return new SubmitResult(false, null, "Covered song must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(performedBy))
        {
            return new SubmitResult(false, null, "Performed by is required.");
        }

        if (performedBy.Trim().Length > 200)
        {
            return new SubmitResult(false, null, "Performed by must be 200 characters or fewer.");
        }

        if (description is { Length: > 0 } && description.Trim().Length > 2000)
        {
            return new SubmitResult(false, null, "Description must be 2000 characters or fewer.");
        }

        var upload = await audioUploadService.UploadPendingAsync(
            memberAccountId,
            audioStream,
            originalFileName,
            cancellationToken);
        if (!upload.Succeeded || upload.Blob is null)
        {
            return new SubmitResult(false, null, upload.Error ?? "Could not upload the audio file.");
        }

        if (!string.Equals(upload.Blob.Container, BlobUploadContainers.FanPerformances, StringComparison.Ordinal))
        {
            return new SubmitResult(false, null, "Audio must be stored in the pending fan-performance container.");
        }

        var created = await fanPerformanceSubmissionRepository.CreateAsync(
            new NewFanPerformanceSubmission(
                memberAccountId,
                title.Trim(),
                coveredSong.Trim(),
                performedBy.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                upload.Blob.BlobName,
                Path.GetFileName(originalFileName),
                upload.Blob.SizeBytes,
                upload.Blob.ContentType,
                upload.DurationSeconds,
                DateTimeOffset.UtcNow,
                FanPerformanceSubmissionRights.DeclarationVersion),
            cancellationToken);

        return new SubmitResult(true, created, null);
    }

    public async Task<ActionResult> WithdrawAsync(
        Guid memberAccountId,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var submission = await fanPerformanceSubmissionRepository.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null || submission.SubmitterMemberId != memberAccountId)
        {
            return new ActionResult(false, null, "Submission was not found.");
        }

        if (!FanPerformanceSubmissionWorkflow.CanMemberWithdraw(submission.Status))
        {
            return new ActionResult(false, submission, "This submission can no longer be withdrawn.");
        }

        FanPerformanceSubmission? updated;
        try
        {
            updated = await fanPerformanceSubmissionRepository.UpdateStatusAsync(
                submissionId,
                FanPerformanceSubmissionStatus.Withdrawn,
                actorEmail: string.Empty,
                reviewNotes: null,
                rejectionReason: null,
                auditDetails: "Member withdrew the submission.",
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new ActionResult(false, submission, ex.Message);
        }

        if (updated is null)
        {
            return new ActionResult(false, null, "Submission was not found.");
        }

        try
        {
            await blobUploadService.DeleteAsync(
                BlobUploadContainers.FanPerformances,
                updated.BlobPath,
                cancellationToken);
        }
        catch (BlobUploadException)
        {
            // Status is already withdrawn; pending audio is unreachable from public pages.
        }

        return new ActionResult(true, updated, null);
    }

    public async Task<ActionResult> ReplyNeedsInfoAsync(
        Guid memberAccountId,
        Guid submissionId,
        string? reply,
        CancellationToken cancellationToken = default)
    {
        var submission = await fanPerformanceSubmissionRepository.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null || submission.SubmitterMemberId != memberAccountId)
        {
            return new ActionResult(false, null, "Submission was not found.");
        }

        if (!FanPerformanceSubmissionWorkflow.CanMemberReplyNeedsInfo(submission.Status))
        {
            return new ActionResult(false, submission, "This submission is not waiting for more information.");
        }

        if (string.IsNullOrWhiteSpace(reply))
        {
            return new ActionResult(false, submission, "Reply is required.");
        }

        var trimmed = reply.Trim();
        if (trimmed.Length > MaxReplyLength)
        {
            return new ActionResult(false, submission, $"Reply must be {MaxReplyLength} characters or fewer.");
        }

        try
        {
            var updated = await fanPerformanceSubmissionRepository.UpdateStatusAsync(
                submissionId,
                FanPerformanceSubmissionStatus.UnderReview,
                actorEmail: string.Empty,
                reviewNotes: null,
                rejectionReason: null,
                auditDetails: trimmed,
                cancellationToken);
            return updated is null
                ? new ActionResult(false, null, "Submission was not found.")
                : new ActionResult(true, updated, null);
        }
        catch (InvalidOperationException ex)
        {
            return new ActionResult(false, submission, ex.Message);
        }
    }
}
