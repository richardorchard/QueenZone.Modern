using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryFanPerformanceSubmissionRepository : IFanPerformanceSubmissionRepository
{
    private readonly object sync = new();
    private readonly List<FanPerformanceSubmissionEntity> submissions = [];
    private readonly List<FanPerformanceSubmissionAuditLogEntity> auditLogs = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;
    private long nextAuditId = 1;

    public InMemoryFanPerformanceSubmissionRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<FanPerformanceSubmission> CreateAsync(
        NewFanPerformanceSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        lock (sync)
        {
            var entity = CreateEntity(submission);
            submissions.Add(entity);
            auditLogs.Add(new FanPerformanceSubmissionAuditLogEntity
            {
                Id = nextAuditId++,
                FanPerformanceSubmissionId = entity.Id,
                Action = "Submitted",
                ActorEmail = string.Empty,
                OccurredAt = entity.SubmittedAt,
                Details = "Member submitted a fan performance for review.",
            });

            return Task.FromResult(Map(entity));
        }
    }

    public Task<FanPerformanceSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(row => row.Id == id);
            return Task.FromResult(entity is null ? null : Map(entity));
        }
    }

    public Task<SubmissionListPage<FanPerformanceSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        lock (sync)
        {
            var owned = submissions
                .Where(row => row.SubmitterMemberId == submitterMemberId)
                .OrderByDescending(row => row.SubmittedAt)
                .ToList();

            IReadOnlyList<FanPerformanceSubmission> items = owned
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Map)
                .ToList();

            return Task.FromResult(new SubmissionListPage<FanPerformanceSubmission>(items, owned.Count));
        }
    }

    public Task<FanPerformanceSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? actorEmail,
        string? reviewNotes,
        string? rejectionReason,
        string? auditDetails = null,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(row => row.Id == id);
            if (entity is null)
            {
                return Task.FromResult<FanPerformanceSubmission?>(null);
            }

            ApplyStatusChange(entity, status, actorEmail, reviewNotes, rejectionReason);
            auditLogs.Add(new FanPerformanceSubmissionAuditLogEntity
            {
                Id = nextAuditId++,
                FanPerformanceSubmissionId = entity.Id,
                Action = entity.Status,
                ActorEmail = entity.ReviewerEmail ?? string.Empty,
                OccurredAt = entity.ReviewedAt ?? DateTimeOffset.UtcNow,
                Details = auditDetails ?? BuildAuditDetails(entity.Status, entity),
            });

            return Task.FromResult<FanPerformanceSubmission?>(Map(entity));
        }
    }

    /// <summary>Test helper: audit entries written for a submission.</summary>
    public IReadOnlyList<FanPerformanceSubmissionAuditLogEntity> GetAuditLogs(Guid submissionId)
    {
        lock (sync)
        {
            return auditLogs.Where(log => log.FanPerformanceSubmissionId == submissionId).ToList();
        }
    }

    private FanPerformanceSubmission Map(FanPerformanceSubmissionEntity entity)
    {
        var member = resolveMember?.Invoke(entity.SubmitterMemberId);
        return Map(entity, member?.DisplayName, member?.Email);
    }

    internal static FanPerformanceSubmissionEntity CreateEntity(NewFanPerformanceSubmission submission) =>
        new()
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

    internal static void ApplyStatusChange(
        FanPerformanceSubmissionEntity entity,
        string status,
        string? actorEmail,
        string? reviewNotes,
        string? rejectionReason)
    {
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
    }

    internal static string? BuildAuditDetails(string status, FanPerformanceSubmissionEntity entity) =>
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

    internal static FanPerformanceSubmission Map(
        FanPerformanceSubmissionEntity entity,
        string? displayName,
        string? email) =>
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
            displayName,
            email);

    internal static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
