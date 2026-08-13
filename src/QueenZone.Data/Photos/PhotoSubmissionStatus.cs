namespace QueenZone.Data;

public static class PhotoSubmissionStatus
{
    public const string Pending = "Pending";
    public const string UnderReview = "UnderReview";
    public const string NeedsInfo = "NeedsInfo";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        UnderReview,
        NeedsInfo,
        Approved,
        Rejected,
    ];

    public static bool IsKnown(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string status)
    {
        var match = All.FirstOrDefault(s =>
            string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
        return match
            ?? throw new ArgumentException($"Unknown photo submission status '{status}'.", nameof(status));
    }
}
