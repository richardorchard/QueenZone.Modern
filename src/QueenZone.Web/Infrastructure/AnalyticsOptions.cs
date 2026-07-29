namespace QueenZone.Web;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public string? MeasurementId { get; init; }

    public string? GoogleAnalyticsPropertyId { get; init; }

    public string? GoogleAnalyticsServiceAccountJson { get; init; }

    public int TrafficCacheMinutes { get; init; } = 60;
}
