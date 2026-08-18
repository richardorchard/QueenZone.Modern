namespace QueenZone.Data;

public static class HelpRequestTopic
{
    public const string Account = "Account";

    public const string Content = "Content";

    public const string Technical = "Technical";

    public const string Privacy = "Privacy";

    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = [Account, Content, Technical, Privacy, Other];

    public static bool IsKnown(string? topic) =>
        !string.IsNullOrWhiteSpace(topic) && All.Contains(topic.Trim(), StringComparer.Ordinal);

    public static string Normalize(string topic)
    {
        var trimmed = topic.Trim();
        foreach (var known in All)
        {
            if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        throw new ArgumentException($"Unknown help request topic '{topic}'.", nameof(topic));
    }

    public static string DisplayName(string topic) =>
        Normalize(topic) switch
        {
            Account => "Account",
            Content => "Content on the site",
            Technical => "Technical problem",
            Privacy => "Privacy / data",
            Other => "Other",
            _ => topic,
        };
}
