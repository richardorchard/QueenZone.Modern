namespace QueenZone.Web;

public sealed class PrivateMessageRateLimitOptions
{
    public const string SectionName = "RateLimiting:PrivateMessages";

    public int WindowMinutes { get; set; } = 10;

    public int MaxMessagesPerWindow { get; set; } = 20;

    /// <summary>
    /// Distinct new conversations (new recipients) a member may start within the window.
    /// Replies in existing conversations are not counted against this limit.
    /// </summary>
    public int MaxNewRecipientsPerWindow { get; set; } = 8;

    /// <summary>
    /// How many times the exact same message body may be sent (across any conversation)
    /// within the window before further identical sends are denied.
    /// </summary>
    public int MaxDuplicateMessagesPerWindow { get; set; } = 3;

    /// <summary>
    /// Accounts younger than this are treated as low-trust and use the stricter
    /// <see cref="NewAccountMaxMessagesPerWindow"/> / <see cref="NewAccountMaxNewRecipientsPerWindow"/> limits.
    /// </summary>
    public int NewAccountAgeDays { get; set; } = 3;

    public int NewAccountMaxMessagesPerWindow { get; set; } = 5;

    public int NewAccountMaxNewRecipientsPerWindow { get; set; } = 3;
}
