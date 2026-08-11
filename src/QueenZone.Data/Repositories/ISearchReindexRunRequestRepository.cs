namespace QueenZone.Data;

public interface ISearchReindexRunRequestRepository
{
    Task<SearchReindexRunRequestQueueResult> QueueAsync(
        SearchReindexRunRequestCreate request,
        CancellationToken cancellationToken = default);

    Task<SearchReindexRunRequest?> ClaimNextAsync(
        string runnerId,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        long requestId,
        string summary,
        CancellationToken cancellationToken = default);

    Task<bool> FailAsync(
        long requestId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<bool> ReturnToPendingAsync(
        long requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchReindexRunRequest>> ListRecentAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);
}
