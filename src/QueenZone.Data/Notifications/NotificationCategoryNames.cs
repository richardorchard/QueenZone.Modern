using QueenZone.Data.Entities;

namespace QueenZone.Data;

/// <summary>
/// Stable <c>data.category</c> strings for the #757 push payload contract.
/// </summary>
public static class NotificationCategoryNames
{
    public const string ForumReply = "forumReply";

    public const string PrivateMessage = "privateMessage";

    public const string News = "news";

    public static string ToPayloadValue(this NotificationCategory category) => category switch
    {
        NotificationCategory.ForumReply => ForumReply,
        NotificationCategory.PrivateMessage => PrivateMessage,
        NotificationCategory.News => News,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };
}
