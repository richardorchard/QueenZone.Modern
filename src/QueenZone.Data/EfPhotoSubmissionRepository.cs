using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfPhotoSubmissionRepository(QueenZoneDbContext dbContext) : IPhotoSubmissionRepository
{
    public async Task<PhotoSubmission> CreateAsync(
        NewPhotoSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

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

        entity.AuditLogs.Add(new PhotoSubmissionAuditLogEntity
        {
            PhotoSubmissionId = entity.Id,
            Action = "Submitted",
            ActorEmail = string.Empty,
            OccurredAt = entity.SubmittedAt,
            Details = "Member submitted photo for review.",
        });

        dbContext.PhotoSubmissions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<PhotoSubmissionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.PhotoSubmissions
            .AsNoTracking()
            .Where(row =>
                row.Status == PhotoSubmissionStatus.Pending
                || row.Status == PhotoSubmissionStatus.UnderReview
                || row.Status == PhotoSubmissionStatus.NeedsInfo)
            .OrderByDescending(row => row.SubmittedAt);

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new
            {
                row.Id,
                row.Title,
                row.SubmitterMemberId,
                DisplayName = row.Submitter != null ? row.Submitter.DisplayName : string.Empty,
                row.SubmittedAt,
                row.SuggestedCategory,
                row.Status,
                row.ThumbnailBlobPath,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new PhotoSubmissionListItem(
                row.Id,
                row.Title,
                row.SubmitterMemberId,
                string.IsNullOrWhiteSpace(row.DisplayName) ? "Unknown member" : row.DisplayName,
                row.SubmittedAt,
                row.SuggestedCategory,
                row.Status,
                row.ThumbnailBlobPath))
            .ToList();
    }

    public async Task<PhotoSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PhotoSubmissions
            .AsNoTracking()
            .Include(row => row.Submitter)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<PhotoSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.PhotoSubmissions
            .AsNoTracking()
            .Where(row => row.SubmitterMemberId == submitterMemberId)
            .OrderByDescending(row => row.SubmittedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<PhotoSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? reviewNotes,
        string? rejectionReason,
        string? approvedCategory = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PhotoSubmissions
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
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

        dbContext.PhotoSubmissionAuditLogs.Add(new PhotoSubmissionAuditLogEntity
        {
            PhotoSubmissionId = entity.Id,
            Action = next,
            ActorEmail = entity.ReviewerEmail ?? string.Empty,
            OccurredAt = entity.ReviewedAt.Value,
            Details = BuildAuditDetails(next, entity),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static string? BuildAuditDetails(string status, PhotoSubmissionEntity entity) =>
        status switch
        {
            PhotoSubmissionStatus.Approved =>
                $"Approved for category '{entity.ApprovedCategory}'. Notes: {entity.ReviewNotes ?? "(none)"}",
            PhotoSubmissionStatus.Rejected =>
                $"Rejected. Reason: {entity.RejectionReason}. Notes: {entity.ReviewNotes ?? "(none)"}",
            PhotoSubmissionStatus.NeedsInfo =>
                $"Needs info. Notes: {entity.ReviewNotes ?? "(none)"}",
            _ => entity.ReviewNotes,
        };

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static PhotoSubmission Map(PhotoSubmissionEntity entity) =>
        new(
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
            entity.Submitter?.DisplayName,
            entity.Submitter?.Email);
}
