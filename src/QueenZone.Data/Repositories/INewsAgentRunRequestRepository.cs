namespace QueenZone.Data;

public interface INewsAgentRunRequestRepository
{
    Task<NewsAgentRunRequestQueueResult> QueueAsync(
        string requestedBy,
        CancellationToken cancellationToken = default);

    Task<NewsAgentRunRequest?> ClaimNextAsync(
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

    Task RecordHeartbeatAsync(
        string runnerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsAgentRunRequest>> ListRecentAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<NewsAgentRunnerHeartbeat?> GetLatestHeartbeatAsync(
        CancellationToken cancellationToken = default);
}
