namespace QueenZone.Data;

public static class HelpRequestStatus
{
    public const string Open = "Open";

    public const string InProgress = "InProgress";

    public const string Resolved = "Resolved";

    public const string Spam = "Spam";

    public static readonly IReadOnlyList<string> All = [Open, InProgress, Resolved, Spam];

    public static bool IsOpenQueue(string status) =>
        Normalize(status) is Open or InProgress;

    public static string Normalize(string status) =>
        status switch
        {
            Open => Open,
            InProgress => InProgress,
            Resolved => Resolved,
            Spam => Spam,
            _ => throw new ArgumentException($"Unknown help request status '{status}'.", nameof(status)),
        };

    public static string DisplayName(string status) =>
        Normalize(status) switch
        {
            Open => "Open",
            InProgress => "In progress",
            Resolved => "Resolved",
            Spam => "Spam",
            _ => status,
        };
}
