namespace QueenZone.Data.Entities;

public sealed class NewsAgentRunnerHeartbeatEntity
{
    public string RunnerId { get; set; } = string.Empty;

    public DateTime LastSeenAtUtc { get; set; }

    public DateTime? LastClaimedAtUtc { get; set; }
}
