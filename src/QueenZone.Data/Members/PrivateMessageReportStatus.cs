namespace QueenZone.Data;

/// <summary>
/// Workflow statuses for <see cref="Entities.PrivateMessageReportEntity"/>.
/// Default at creation is <see cref="Open"/>; later transitions are the
/// moderator review surface (issue #470).
/// </summary>
public static class PrivateMessageReportStatus
{
    public const string Open = "Open";

    public const string Reviewed = "Reviewed";

    public const string Dismissed = "Dismissed";

    public const string Actioned = "Actioned";

    public static readonly IReadOnlyList<string> All =
    [
        Open,
        Reviewed,
        Dismissed,
        Actioned,
    ];

    public static bool IsKnown(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string status)
    {
        var match = All.FirstOrDefault(s =>
            string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
        return match
            ?? throw new ArgumentException($"Unknown private-message report status '{status}'.", nameof(status));
    }
}
