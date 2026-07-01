namespace QueenZone.NewsAgent;

public sealed class NewsAgentSchedulerOptions
{
    public const string SectionName = "NewsAgentScheduler";

    public bool UseRunLease { get; init; } = true;

    public string LeaseName { get; init; } = "discover-news";

    public int LeaseDurationMinutes { get; init; } = 120;
}
