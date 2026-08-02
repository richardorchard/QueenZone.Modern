namespace QueenZone.Data;

public enum NewsAgentRunRequestStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public sealed record NewsAgentRunRequest(
    long Id,
    NewsAgentRunRequestStatus Status,
    string RequestedBy,
    DateTime RequestedAtUtc,
    string? RunnerId,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Summary,
    string? ErrorMessage);

public sealed record NewsAgentRunRequestQueueResult(
    NewsAgentRunRequest Request,
    bool WasCreated);

public sealed record NewsAgentRunnerHeartbeat(
    string RunnerId,
    DateTime LastSeenAtUtc,
    DateTime? LastClaimedAtUtc);
