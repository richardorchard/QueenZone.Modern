namespace QueenZone.Data;

public record SubmissionTypeCounts(
    int Pending,
    int ReceivedToday,
    int ReceivedThisWeek,
    int ApprovedLast30Days,
    int RejectedLast30Days,
    int StillPendingFromLast30Days)
{
    public static readonly SubmissionTypeCounts Empty =
        new(0, 0, 0, 0, 0, 0);
}
