namespace QueenZone.Data;

public enum NewsAgentRunRequestStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public enum NewsAgentRunRequestKind
{
    ScheduledGathering = 0,
    UrlIngestion = 1
}

public sealed record NewsAgentRunRequest(
    long Id,
    NewsAgentRunRequestStatus Status,
    NewsAgentRunRequestKind Kind,
    string RequestedBy,
    DateTime RequestedAtUtc,
    string? ArticleUrl,
    bool GenerateDraft,
    string? RunnerId,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Summary,
    string? ErrorMessage);

public sealed record NewsAgentRunRequestCreate(
    string RequestedBy,
    NewsAgentRunRequestKind Kind = NewsAgentRunRequestKind.ScheduledGathering,
    string? ArticleUrl = null,
    bool GenerateDraft = false);

public sealed record NewsAgentRunRequestQueueResult(
    NewsAgentRunRequest Request,
    bool WasCreated);

public sealed record NewsAgentRunnerHeartbeat(
    string RunnerId,
    DateTime LastSeenAtUtc,
    DateTime? LastClaimedAtUtc);
