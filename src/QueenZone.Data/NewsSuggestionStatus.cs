namespace QueenZone.Data;

public static class NewsSuggestionStatus
{
    public const string Pending = "Pending";

    public const string UnderReview = "UnderReview";

    public const string Promoted = "Promoted";

    public const string Rejected = "Rejected";

    public const string Duplicate = "Duplicate";

    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.Ordinal)
    {
        Pending,
        UnderReview,
    };

    public static bool IsActive(string status) =>
        ActiveStatuses.Contains(Normalize(status));

    public static string Normalize(string status) =>
        status switch
        {
            Pending => Pending,
            UnderReview => UnderReview,
            Promoted => Promoted,
            Rejected => Rejected,
            Duplicate => Duplicate,
            _ => throw new ArgumentException($"Unknown news suggestion status '{status}'.", nameof(status)),
        };
}
