using QueenZone.Data;

namespace QueenZone.Web;

public sealed record NotificationPreferencesResponse(bool ForumReply, bool PrivateMessage, bool News)
{
    public static NotificationPreferencesResponse From(NotificationPreferences preferences) =>
        new(preferences.ForumReply, preferences.PrivateMessage, preferences.News);
}

public sealed record NotificationPreferencePatchRequest(bool? ForumReply, bool? PrivateMessage, bool? News)
{
    public NotificationPreferencePatch ToPatch() => new(ForumReply, PrivateMessage, News);
}
