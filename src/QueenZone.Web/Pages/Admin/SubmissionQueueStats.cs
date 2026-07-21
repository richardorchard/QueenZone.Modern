using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin;

public record SubmissionQueueStats(
    SubmissionTypeCounts Photos,
    SubmissionTypeCounts NewsSuggestions,
    SubmissionTypeCounts Articles,
    IReadOnlyList<SubmissionContributor> TopContributors)
{
    public static readonly SubmissionQueueStats Empty =
        new(SubmissionTypeCounts.Empty, SubmissionTypeCounts.Empty, SubmissionTypeCounts.Empty, []);

    public int TotalApprovedLast30Days =>
        Photos.ApprovedLast30Days + NewsSuggestions.ApprovedLast30Days + Articles.ApprovedLast30Days;

    public int TotalRejectedLast30Days =>
        Photos.RejectedLast30Days + NewsSuggestions.RejectedLast30Days + Articles.RejectedLast30Days;

    public int TotalStillPendingLast30Days =>
        Photos.StillPendingFromLast30Days + NewsSuggestions.StillPendingFromLast30Days + Articles.StillPendingFromLast30Days;

    public int TotalLast30Days =>
        TotalApprovedLast30Days + TotalRejectedLast30Days + TotalStillPendingLast30Days;
}
