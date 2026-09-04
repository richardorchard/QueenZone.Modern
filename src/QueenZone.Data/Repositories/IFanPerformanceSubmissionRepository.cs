namespace QueenZone.Data;

public interface IFanPerformanceSubmissionRepository
{
    Task<FanPerformanceSubmission> CreateAsync(
        NewFanPerformanceSubmission submission,
        CancellationToken cancellationToken = default);

    Task<FanPerformanceSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubmissionListPage<FanPerformanceSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
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
}
