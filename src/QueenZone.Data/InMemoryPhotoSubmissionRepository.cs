using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryPhotoSubmissionRepository : IPhotoSubmissionRepository
{
    private readonly object sync = new();
    private readonly List<PhotoSubmissionEntity> submissions = [];
    private readonly List<PhotoSubmissionAuditLogEntity> auditLogs = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;
    private long nextAuditId = 1;

    public InMemoryPhotoSubmissionRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<PhotoSubmission> CreateAsync(
        NewPhotoSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        lock (sync)
        {
            var entity = new PhotoSubmissionEntity
            {
                Id = submission.Id is { } preferredId && preferredId != Guid.Empty
                    ? preferredId
                    : Guid.NewGuid(),
                SubmitterMemberId = submission.SubmitterMemberId,
                Title = submission.Title.Trim(),
                Description = NormalizeOptional(submission.Description, 1000),
                SuggestedCategory = NormalizeOptional(submission.SuggestedCategory, 100),
                ApproximateYear = submission.ApproximateYear,
                ApproximateDate = submission.ApproximateDate,
                BlobPath = submission.BlobPath.Trim(),
                WebOptimizedBlobPath = submission.WebOptimizedBlobPath.Trim(),
                ThumbnailBlobPath = submission.ThumbnailBlobPath.Trim(),
                OriginalFileName = submission.OriginalFileName.Trim(),
                FileSizeBytes = submission.FileSizeBytes,
                MimeType = submission.MimeType.Trim(),
                ImageWidthPx = submission.ImageWidthPx,
                ImageHeightPx = submission.ImageHeightPx,
                Status = PhotoSubmissionStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow,
            };

            submissions.Add(entity);
            auditLogs.Add(new PhotoSubmissionAuditLogEntity
            {
                Id = nextAuditId++,
                PhotoSubmissionId = entity.Id,
                Action = "Submitted",
                ActorEmail = string.Empty,
                OccurredAt = entity.SubmittedAt,
                Details = "Member submitted photo for review.",
            });

            return Task.FromResult(Map(entity));
        }
    }

    public Task<IReadOnlyList<PhotoSubmissionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        lock (sync)
        {
            IReadOnlyList<PhotoSubmissionListItem> result = submissions
                .Where(row =>
                    row.Status is PhotoSubmissionStatus.Pending
                        or PhotoSubmissionStatus.UnderReview
                        or PhotoSubmissionStatus.NeedsInfo)
                .OrderByDescending(row => row.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row =>
                {
                    var member = resolveMember?.Invoke(row.SubmitterMemberId);
                    return new PhotoSubmissionListItem(
                        row.Id,
                        row.Title,
                        row.SubmitterMemberId,
                        member?.DisplayName ?? "Unknown member",
                        row.SubmittedAt,
                        row.SuggestedCategory,
                        row.Status,
                        row.ThumbnailBlobPath);
                })
                .ToList();

            return Task.FromResult(result);
        }
    }

    public Task<PhotoSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(row => row.Id == id);
            return Task.FromResult(entity is null ? null : Map(entity));
        }
    }

    public Task<IReadOnlyList<PhotoSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            IReadOnlyList<PhotoSubmission> result = submissions
                .Where(row => row.SubmitterMemberId == submitterMemberId)
                .OrderByDescending(row => row.SubmittedAt)
                .Select(Map)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<PhotoSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? reviewNotes,
        string? rejectionReason,
        string? approvedCategory = null,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(row => row.Id == id);
            if (entity is null)
            {
                return Task.FromResult<PhotoSubmission?>(null);
            }

            if (!PhotoSubmissionWorkflow.TryValidateStatusChange(entity.Status, status, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var next = PhotoSubmissionStatus.Normalize(status);
            entity.Status = next;
            entity.ReviewedAt = DateTimeOffset.UtcNow;
            entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
            entity.ReviewNotes = NormalizeOptional(reviewNotes, 500);

            if (next == PhotoSubmissionStatus.Rejected)
            {
                entity.RejectionReason = NormalizeOptional(rejectionReason, 500)
                    ?? throw new InvalidOperationException("A rejection reason is required.");
            }
            else if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                entity.RejectionReason = NormalizeOptional(rejectionReason, 500);
            }

            if (next == PhotoSubmissionStatus.Approved)
            {
                var category = NormalizeOptional(approvedCategory, 100)
                    ?? NormalizeOptional(entity.SuggestedCategory, 100);
                entity.ApprovedCategory = category
                    ?? throw new InvalidOperationException("An approved gallery category is required.");
            }
            else if (!string.IsNullOrWhiteSpace(approvedCategory))
            {
                entity.ApprovedCategory = NormalizeOptional(approvedCategory, 100);
            }

            auditLogs.Add(new PhotoSubmissionAuditLogEntity
            {
                Id = nextAuditId++,
                PhotoSubmissionId = entity.Id,
                Action = next,
                ActorEmail = entity.ReviewerEmail ?? string.Empty,
                OccurredAt = entity.ReviewedAt.Value,
                Details = next switch
                {
                    PhotoSubmissionStatus.Approved =>
                        $"Approved for category '{entity.ApprovedCategory}'. Notes: {entity.ReviewNotes ?? "(none)"}",
                    PhotoSubmissionStatus.Rejected =>
                        $"Rejected. Reason: {entity.RejectionReason}. Notes: {entity.ReviewNotes ?? "(none)"}",
                    PhotoSubmissionStatus.NeedsInfo =>
                        $"Needs info. Notes: {entity.ReviewNotes ?? "(none)"}",
                    _ => entity.ReviewNotes,
                },
            });

            return Task.FromResult<PhotoSubmission?>(Map(entity));
        }
    }

    /// <summary>Test helper: audit entries written for a submission.</summary>
    public IReadOnlyList<PhotoSubmissionAuditLogEntity> GetAuditLogs(Guid submissionId)
    {
        lock (sync)
        {
            return auditLogs.Where(log => log.PhotoSubmissionId == submissionId).ToList();
        }
    }

    private PhotoSubmission Map(PhotoSubmissionEntity entity)
    {
        var member = resolveMember?.Invoke(entity.SubmitterMemberId);
        return new PhotoSubmission(
            entity.Id,
            entity.SubmitterMemberId,
            entity.Title,
            entity.Description,
            entity.SuggestedCategory,
            entity.ApprovedCategory,
            entity.ApproximateYear,
            entity.ApproximateDate,
            entity.BlobPath,
            entity.WebOptimizedBlobPath,
            entity.ThumbnailBlobPath,
            entity.OriginalFileName,
            entity.FileSizeBytes,
            entity.MimeType,
            entity.ImageWidthPx,
            entity.ImageHeightPx,
            entity.Status,
            entity.SubmittedAt,
            entity.ReviewedAt,
            entity.ReviewerEmail,
            entity.ReviewNotes,
            entity.RejectionReason,
            member?.DisplayName,
            member?.Email);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
