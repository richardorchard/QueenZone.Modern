namespace QueenZone.Data;

public enum SearchReindexRunRequestStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public sealed record SearchReindexRunRequest(
    long Id,
    SearchReindexRunRequestStatus Status,
    string RequestedBy,
    DateTime RequestedAtUtc,
    string? RunnerId,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Summary,
    string? ErrorMessage);

public sealed record SearchReindexRunRequestCreate(string RequestedBy);

public sealed record SearchReindexRunRequestQueueResult(
    SearchReindexRunRequest Request,
    bool WasCreated);
