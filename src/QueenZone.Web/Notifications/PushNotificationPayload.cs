using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// #757 push contract: a short human title/body plus a stable <c>data</c> dictionary.
/// Mobile deep-links from these keys — do not invent parallel shapes per category.
/// </summary>
public sealed record PushNotificationPayload(
    string Category,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data)
{
    public const int MaxAlertLength = 120;

    public static PushNotificationPayload ForumReply(int topicId, int postId, string topicTitle)
    {
        var title = Truncate(topicTitle, fallback: "New forum reply");
        return new(
            NotificationCategoryNames.ForumReply,
            title,
            "New reply",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["category"] = NotificationCategoryNames.ForumReply,
                ["topicId"] = topicId.ToString(),
                ["postId"] = postId.ToString(),
            });
    }

    public static PushNotificationPayload PrivateMessage(Guid conversationId) =>
        new(
            NotificationCategoryNames.PrivateMessage,
            "New private message",
            "You have a new message.",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["category"] = NotificationCategoryNames.PrivateMessage,
                ["conversationId"] = conversationId.ToString(),
            });

    public static PushNotificationPayload News(int articleId, string articleTitle)
    {
        var title = Truncate(articleTitle, fallback: "New QueenZone article");
        return new(
            NotificationCategoryNames.News,
            title,
            "New article published.",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["category"] = NotificationCategoryNames.News,
                ["articleId"] = articleId.ToString(),
            });
    }

    private static string Truncate(string? value, string fallback)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return fallback;
        }

        return trimmed.Length <= MaxAlertLength
            ? trimmed
            : trimmed[..MaxAlertLength].TrimEnd();
    }
}
