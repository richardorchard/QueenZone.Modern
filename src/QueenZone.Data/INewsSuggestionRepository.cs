namespace QueenZone.Data;

public interface INewsSuggestionRepository
{
    Task<NewsSuggestion> CreateAsync(NewsSuggestion suggestion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsSuggestionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<NewsSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NewsSuggestion?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the same normalized URL is already pending or under review.
    /// </summary>
    Task<bool> HasActiveDuplicateAsync(string urlHash, CancellationToken cancellationToken = default);

    Task<int> CountBySubmitterSinceAsync(
        Guid submitterMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    Task<NewsSuggestion?> PromoteAsync(
        Guid id,
        int promotedNewsId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    Task<NewsSuggestion?> MarkDuplicateAsync(
        Guid id,
        int duplicateCandidateId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default);
}
