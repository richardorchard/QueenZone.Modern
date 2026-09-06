namespace QueenZone.Web;

public sealed class FanPerformanceRateLimitingOptions
{
    public const string SectionName = "RateLimiting:FanPerformances";
    public const string AudioPolicy = "fan-performance-audio";
    public const string BrowsePolicy = "fan-performances-browse";

    // Range-processed streaming (expo-audio / browsers) issues many 206s per play.
    // 10/5min was enough to wedge live playback after a download probe + retry burst.
    public int AudioPermitLimit { get; set; } = 60;
    public int AudioSlidingWindowSeconds { get; set; } = 300;
    public int BrowsePermitLimit { get; set; } = 60;
    public int BrowseWindowSeconds { get; set; } = 60;
}
