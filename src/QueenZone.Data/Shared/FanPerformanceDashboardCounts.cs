namespace QueenZone.Data;

public sealed record FanPerformanceDashboardCounts(
    SubmissionTypeCounts Queue,
    int StalePendingCount,
    int? OldestOpenAgeDays)
{
    public const int DefaultStaleAfterDays = 7;

    public static readonly FanPerformanceDashboardCounts Empty =
        new(SubmissionTypeCounts.Empty, 0, null);

    public int Pending => Queue.Pending;

    public int ReceivedToday => Queue.ReceivedToday;

    public int ReceivedThisWeek => Queue.ReceivedThisWeek;

    public int ApprovedLast30Days => Queue.ApprovedLast30Days;

    public int RejectedLast30Days => Queue.RejectedLast30Days;

    public int StillPendingFromLast30Days => Queue.StillPendingFromLast30Days;
}
