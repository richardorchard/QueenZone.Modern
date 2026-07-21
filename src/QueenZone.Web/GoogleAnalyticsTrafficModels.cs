namespace QueenZone.Web;

public sealed record GoogleAnalyticsTrafficSnapshot(
    bool IsAvailable,
    int SessionsLast7Days,
    int PageViewsLast7Days,
    int ActiveUsersLast7Days,
    IReadOnlyList<GoogleAnalyticsTopPage> TopPagesThisWeek,
    IReadOnlyList<GoogleAnalyticsDailySession> DailySessionsLast30Days,
    string? UnavailableReason = null)
{
    public static GoogleAnalyticsTrafficSnapshot Unavailable(string reason) =>
        new(false, 0, 0, 0, [], [], reason);
}

public sealed record GoogleAnalyticsTopPage(string Path, int Views);

public sealed record GoogleAnalyticsDailySession(DateOnly Date, int Sessions);

