namespace QueenZone.Data;

public sealed record NewsAiPipelineHealth(
    int RunsLast24Hours,
    DateTime? LastSuccessfulRunAtUtc,
    int ErrorCountLast24Hours)
{
    public static readonly NewsAiPipelineHealth Empty = new(0, null, 0);

    public bool IsStale(DateTime utcNow) =>
        LastSuccessfulRunAtUtc is null
        || utcNow - LastSuccessfulRunAtUtc.Value > TimeSpan.FromHours(25);
}
