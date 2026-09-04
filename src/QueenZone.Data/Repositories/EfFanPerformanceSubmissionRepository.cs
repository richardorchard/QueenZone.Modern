using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfFanPerformanceSubmissionRepository(QueenZoneDbContext dbContext)
    : IFanPerformanceSubmissionRepository
{
    public async Task<FanPerformanceSubmission> CreateAsync(
        NewFanPerformanceSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var entity = new FanPerformanceSubmissionEntity
        {
            Id = submission.Id is { } preferredId && preferredId != Guid.Empty
                ? preferredId
                : Guid.NewGuid(),
            SubmitterMemberId = submission.SubmitterMemberId,
            Title = submission.Title.Trim(),
            CoveredSong = submission.CoveredSong.Trim(),
            PerformedBy = submission.PerformedBy.Trim(),
            Description = NormalizeOptional(submission.Description, 2000),
            BlobPath = submission.BlobPath.Trim(),
            OriginalFileName = submission.OriginalFileName.Trim(),
            FileSizeBytes = submission.FileSizeBytes,
            MimeType = submission.MimeType.Trim(),
            DurationSeconds = submission.DurationSeconds,
            Status = FanPerformanceSubmissionStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            RightsDeclaredAt = submission.RightsDeclaredAt,
            RightsDeclarationVersion = submission.RightsDeclarationVersion.Trim(),
        };

        entity.AuditLogs.Add(new FanPerformanceSubmissionAuditLogEntity
        {
            FanPerformanceSubmissionId = entity.Id,
            Action = "Submitted",
            ActorEmail = string.Empty,
            OccurredAt = entity.SubmittedAt,
            Details = "Member submitted a fan performance for review.",
        });

        dbContext.FanPerformanceSubmissions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<FanPerformanceSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FanPerformanceSubmissions
            .AsNoTracking()
            .Include(row => row.Submitter)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<SubmissionListPage<FanPerformanceSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.FanPerformanceSubmissions
            .AsNoTracking()
            .Where(row => row.SubmitterMemberId == submitterMemberId);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        if (IsSqliteDatabase())
        {
            var sqliteRows = await query
                .Select(row => new
                {
                    row.Id,
                    row.SubmitterMemberId,
                    row.Title,
                    row.CoveredSong,
                    row.PerformedBy,
                    row.Description,
                    row.BlobPath,
                    row.OriginalFileName,
                    row.FileSizeBytes,
                    row.MimeType,
                    row.DurationSeconds,
                    row.Status,
                    row.SubmittedAt,
                    row.ReviewedAt,
                    row.ReviewerEmail,
                    row.ReviewNotes,
                    row.RejectionReason,
                    row.RightsDeclaredAt,
                    row.RightsDeclarationVersion,
                    row.PromotedStageId,
                    DisplayName = row.Submitter != null ? row.Submitter.DisplayName : null,
                    Email = row.Submitter != null ? row.Submitter.Email : null,
                })
                .ToListAsync(cancellationToken);

            var sqliteItems = sqliteRows
                .OrderByDescending(row => row.SubmittedAt)
                .ThenBy(row => row.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(row => new FanPerformanceSubmission(
                    row.Id,
                    row.SubmitterMemberId,
                    row.Title,
                    row.CoveredSong,
                    row.PerformedBy,
                    row.Description,
                    row.BlobPath,
                    row.OriginalFileName,
                    row.FileSizeBytes,
                    row.MimeType,
                    row.DurationSeconds,
                    row.Status,
                    row.SubmittedAt,
                    row.ReviewedAt,
                    row.ReviewerEmail,
                    row.ReviewNotes,
                    row.RejectionReason,
                    row.RightsDeclaredAt,
                    row.RightsDeclarationVersion,
                    row.PromotedStageId,
                    row.DisplayName,
                    row.Email))
                .ToList();
            return new SubmissionListPage<FanPerformanceSubmission>(sqliteItems, totalCount);
        }

        var items = await MemberQueueQuery(submitterMemberId, skip, pageSize).ToListAsync(cancellationToken);
        return new SubmissionListPage<FanPerformanceSubmission>(items, totalCount);
    }

    public async Task<FanPerformanceSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? actorEmail,
        string? reviewNotes,
        string? rejectionReason,
        string? auditDetails = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FanPerformanceSubmissions
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!FanPerformanceSubmissionWorkflow.TryValidateStatusChange(entity.Status, status, out var error))
        {
            throw new InvalidOperationException(error);
        }

        var next = FanPerformanceSubmissionStatus.Normalize(status);
        entity.Status = next;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(actorEmail))
        {
            entity.ReviewerEmail = NormalizeOptional(actorEmail, 256);
        }

        if (reviewNotes is not null)
        {
            entity.ReviewNotes = NormalizeOptional(reviewNotes, 500);
        }

        if (next == FanPerformanceSubmissionStatus.Rejected)
        {
            entity.RejectionReason = NormalizeOptional(rejectionReason, 500)
                ?? throw new InvalidOperationException("A rejection reason is required.");
        }
        else if (!string.IsNullOrWhiteSpace(rejectionReason))
        {
            entity.RejectionReason = NormalizeOptional(rejectionReason, 500);
        }

        dbContext.FanPerformanceSubmissionAuditLogs.Add(new FanPerformanceSubmissionAuditLogEntity
        {
            FanPerformanceSubmissionId = entity.Id,
            Action = next,
            ActorEmail = entity.ReviewerEmail ?? string.Empty,
            OccurredAt = entity.ReviewedAt.Value,
            Details = auditDetails ?? BuildAuditDetails(next, entity),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    internal IQueryable<FanPerformanceSubmission> MemberQueueQuery(Guid submitterMemberId, int skip, int take) =>
        dbContext.FanPerformanceSubmissions
            .AsNoTracking()
            .Where(row => row.SubmitterMemberId == submitterMemberId)
            .OrderByDescending(row => row.SubmittedAt)
            .ThenBy(row => row.Id)
            .Skip(skip)
            .Take(take)
            .Select(row => new FanPerformanceSubmission(
                row.Id,
                row.SubmitterMemberId,
                row.Title,
                row.CoveredSong,
                row.PerformedBy,
                row.Description,
                row.BlobPath,
                row.OriginalFileName,
                row.FileSizeBytes,
                row.MimeType,
                row.DurationSeconds,
                row.Status,
                row.SubmittedAt,
                row.ReviewedAt,
                row.ReviewerEmail,
                row.ReviewNotes,
                row.RejectionReason,
                row.RightsDeclaredAt,
                row.RightsDeclarationVersion,
                row.PromotedStageId,
                row.Submitter != null ? row.Submitter.DisplayName : null,
                row.Submitter != null ? row.Submitter.Email : null));

    private bool IsSqliteDatabase() =>
        string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);

    private static FanPerformanceSubmission Map(FanPerformanceSubmissionEntity entity) =>
        new(
            entity.Id,
            entity.SubmitterMemberId,
            entity.Title,
            entity.CoveredSong,
            entity.PerformedBy,
            entity.Description,
            entity.BlobPath,
            entity.OriginalFileName,
            entity.FileSizeBytes,
            entity.MimeType,
            entity.DurationSeconds,
            entity.Status,
            entity.SubmittedAt,
            entity.ReviewedAt,
            entity.ReviewerEmail,
            entity.ReviewNotes,
            entity.RejectionReason,
            entity.RightsDeclaredAt,
            entity.RightsDeclarationVersion,
            entity.PromotedStageId,
            entity.Submitter?.DisplayName,
            entity.Submitter?.Email);

    private static string? BuildAuditDetails(string status, FanPerformanceSubmissionEntity entity) =>
        status switch
        {
            FanPerformanceSubmissionStatus.Approved =>
                $"Approved. Notes: {entity.ReviewNotes ?? "(none)"}",
            FanPerformanceSubmissionStatus.Rejected =>
                $"Rejected. Reason: {entity.RejectionReason}. Notes: {entity.ReviewNotes ?? "(none)"}",
            FanPerformanceSubmissionStatus.NeedsInfo =>
                $"Needs info. Notes: {entity.ReviewNotes ?? "(none)"}",
            FanPerformanceSubmissionStatus.Withdrawn =>
                "Member withdrew the submission.",
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
}
