namespace QueenZone.Data;

public static class FanPerformanceReportStatus
{
    public const string Open = "Open";
    public const string Resolved = "Resolved";
    public const string Dismissed = "Dismissed";

    public static readonly IReadOnlyList<string> All = [Open, Resolved, Dismissed];

    public static bool IsKnown(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string status)
    {
        var match = All.FirstOrDefault(item =>
            string.Equals(item, status.Trim(), StringComparison.OrdinalIgnoreCase));
        return match
            ?? throw new ArgumentException($"Unknown fan-performance report status '{status}'.", nameof(status));
    }

    public static bool IsOpen(string? status) =>
        string.Equals(status, Open, StringComparison.OrdinalIgnoreCase);
}
