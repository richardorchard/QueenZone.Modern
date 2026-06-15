namespace QueenZone.Data.Entities;

public sealed class NewsAuditLogEntity
{
    public int Id { get; set; }

    public int NewsId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ActorEmail { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public string? Details { get; set; }
}