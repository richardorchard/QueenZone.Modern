namespace QueenZone.Data;

public interface IFanPerformanceSubmissionRepository
{
    Task<FanPerformanceSubmission> CreateAsync(
        NewFanPerformanceSubmission submission,
        CancellationToken cancellationToken = default);

    Task<FanPerformanceSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FanPerformanceSubmissionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SubmissionListPage<FanPerformanceSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FanPerformanceSubmissionAuditEntry>> GetAuditLogsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates status with transition validation and writes an audit log entry.
    /// </summary>
    /// <returns>The updated submission, or null when not found.</returns>
    /// <exception cref="InvalidOperationException">When the status transition is not allowed.</exception>
    Task<FanPerformanceSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? actorEmail,
        string? reviewNotes,
        string? rejectionReason,
        string? auditDetails = null,
        CancellationToken cancellationToken = default);

    Task<FanPerformanceSubmission?> UpdateReviewMetadataAsync(
        Guid id,
        FanPerformanceReviewEdits edits,
        string editorEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a submission as approved and promoted, recording the Q_STAGE_T id
    /// it was published as and writing an audit log entry.
    /// </summary>
    Task<FanPerformanceSubmission?> PromoteAsync(
        Guid id,
        int promotedStageId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    Task<SubmissionTypeCounts> GetDashboardCountsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsThisMonthAsync(
        DateTimeOffset monthStart,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FanPerformanceSubmission>> GetEligibleForPendingBlobPurgeAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default);

    Task ClearPendingBlobPathAsync(Guid id, CancellationToken cancellationToken = default);
}
