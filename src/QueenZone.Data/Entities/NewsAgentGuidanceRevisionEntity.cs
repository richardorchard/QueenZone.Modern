namespace QueenZone.Data.Entities;

public sealed class NewsAgentGuidanceRevisionEntity
{
    public int Id { get; set; }

    public NewsAgentGuidanceType Type { get; set; }

    public int RevisionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;

    public NewsAgentGuidanceStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedByEmail { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }

    public string? PublishedByEmail { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
