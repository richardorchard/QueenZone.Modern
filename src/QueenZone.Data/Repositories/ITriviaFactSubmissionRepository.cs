namespace QueenZone.Data;

public interface ITriviaFactSubmissionRepository
{
    Task<TriviaFactSubmission> CreateAsync(
        NewTriviaFactSubmission submission,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TriviaFactSubmissionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TriviaFactSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubmissionListPage<TriviaFactSubmission>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a pending suggestion as approved after it has been published as a trivia fact.
    /// </summary>
    Task<TriviaFactSubmission?> ApproveAsync(
        Guid id,
        int promotedTriviaId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a pending suggestion. <paramref name="rejectionReason"/> is shown to the submitter.
    /// </summary>
    Task<TriviaFactSubmission?> RejectAsync(
        Guid id,
        string reviewerEmail,
        string rejectionReason,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    Task<SubmissionTypeCounts> GetDashboardCountsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}
