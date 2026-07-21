namespace QueenZone.Data;

public interface IPhotoSubmissionRepository
{
    Task<PhotoSubmission> CreateAsync(NewPhotoSubmission submission, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhotoSubmissionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PhotoSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubmissionListPage<PhotoSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates status with transition validation and writes an audit log entry.
    /// </summary>
    /// <returns>The updated submission, or null when not found.</returns>
    /// <exception cref="InvalidOperationException">When the status transition is not allowed.</exception>
    Task<PhotoSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? reviewNotes,
        string? rejectionReason,
        string? approvedCategory = null,
        CancellationToken cancellationToken = default);

    Task<SubmissionTypeCounts> GetDashboardCountsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsThisMonthAsync(
        DateTimeOffset monthStart,
        int maxCount,
        CancellationToken cancellationToken = default);
}
