using QueenZone.Data.Entities;

namespace QueenZone.Data;

public readonly record struct NotificationPreferences(bool ForumReply, bool PrivateMessage, bool News)
{
    public static NotificationPreferences Defaults { get; } = new(true, true, false);

    public bool IsEnabled(NotificationCategory category) => category switch
    {
        NotificationCategory.ForumReply => ForumReply,
        NotificationCategory.PrivateMessage => PrivateMessage,
        NotificationCategory.News => News,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };
}

public readonly record struct NotificationPreferencePatch(bool? ForumReply, bool? PrivateMessage, bool? News)
{
    public bool IsEmpty => ForumReply is null && PrivateMessage is null && News is null;
}

public static class NotificationPreferencesMerge
{
    public static NotificationPreferences Resolve(IEnumerable<(NotificationCategory Category, bool Enabled)> choices)
    {
        var result = NotificationPreferences.Defaults;
        foreach (var (category, enabled) in choices)
        {
            result = category switch
            {
                NotificationCategory.ForumReply => result with { ForumReply = enabled },
                NotificationCategory.PrivateMessage => result with { PrivateMessage = enabled },
                NotificationCategory.News => result with { News = enabled },
                _ => result,
            };
        }

        return result;
    }

    public static NotificationPreferences Apply(NotificationPreferences current, NotificationPreferencePatch patch) =>
        new(
            patch.ForumReply ?? current.ForumReply,
            patch.PrivateMessage ?? current.PrivateMessage,
            patch.News ?? current.News);
}
