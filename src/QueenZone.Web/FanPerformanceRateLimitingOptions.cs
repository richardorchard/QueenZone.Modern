namespace QueenZone.Web;

public sealed class FanPerformanceRateLimitingOptions
{
    public const string SectionName = "RateLimiting:FanPerformances";
    public const string AudioPolicy = "fan-performance-audio";
    public const string BrowsePolicy = "fan-performances-browse";

    public int AudioPermitLimit { get; set; } = 10;
    public int AudioSlidingWindowSeconds { get; set; } = 300;
    public int BrowsePermitLimit { get; set; } = 60;
    public int BrowseWindowSeconds { get; set; } = 60;
}
