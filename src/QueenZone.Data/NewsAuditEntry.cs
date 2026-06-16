namespace QueenZone.Data;

public sealed record NewsAuditEntry(
    int Id,
    int NewsId,
    string Action,
    string ActorEmail,
    DateTime OccurredAt,
    string? Details);