using QueenZone.Data;
using QueenZone.Routing;

namespace QueenZone.Web;

/// <summary>
/// Member-facing MySubmissions copy for fan-performance review outcomes.
/// Rejected shows <see cref="FanPerformanceSubmission.RejectionReason"/> only;
/// NeedsInfo shows <see cref="FanPerformanceSubmission.ReviewNotes"/> only;
/// Approved links to the live public surface via <see cref="FanPerformanceSubmission.PromotedStageId"/>.
/// Internal reject notes never appear here.
/// </summary>
public static class FanPerformanceSubmissionFeedback
{
    public static string? GetMemberFacingNotes(FanPerformanceSubmission item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsStatus(item.Status, FanPerformanceSubmissionStatus.Rejected))
        {
            return EmptyToNull(item.RejectionReason);
        }

        if (IsStatus(item.Status, FanPerformanceSubmissionStatus.NeedsInfo))
        {
            return EmptyToNull(item.ReviewNotes);
        }

        return null;
    }

    public static string? GetPublishedPath(FanPerformanceSubmission item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!IsStatus(item.Status, FanPerformanceSubmissionStatus.Approved)
            || item.PromotedStageId is not int promotedStageId)
        {
            return null;
        }

        return FanPerformanceRoutes.GetPublicPath(promotedStageId);
    }

    private static bool IsStatus(string status, string expected) =>
        string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
