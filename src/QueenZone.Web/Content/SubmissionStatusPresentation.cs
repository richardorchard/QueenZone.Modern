using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>Shared status badge labels and CSS modifiers for member and admin submission UIs.</summary>
public static class SubmissionStatusPresentation
{
    public static string GetModifier(string? status) =>
        Normalize(status) switch
        {
            PhotoSubmissionStatus.Pending
                or NewsSuggestionStatus.Pending
                or ArticleSubmissionStatus.Draft
                or ArticleSubmissionStatus.Submitted
                or HelpRequestStatus.Open
                or PrivateMessageReportStatus.Open => "pending",
            PhotoSubmissionStatus.UnderReview
                or NewsSuggestionStatus.UnderReview
                or ArticleSubmissionStatus.UnderReview
                or ArticleSubmissionStatus.ApprovedForPublishing
                or HelpRequestStatus.InProgress
                or PrivateMessageReportStatus.Reviewed => "review",
            PhotoSubmissionStatus.NeedsInfo
                or ArticleSubmissionStatus.RequiresRevision => "attention",
            PhotoSubmissionStatus.Approved
                or NewsSuggestionStatus.Promoted
                or ArticleSubmissionStatus.Published
                or HelpRequestStatus.Resolved
                or PrivateMessageReportStatus.Actioned => "success",
            PhotoSubmissionStatus.Rejected
                or NewsSuggestionStatus.Rejected
                or NewsSuggestionStatus.Duplicate
                or ArticleSubmissionStatus.Rejected
                or HelpRequestStatus.Spam
                or PrivateMessageReportStatus.Dismissed => "danger",
            _ => "neutral",
        };

    public static string GetLabel(string? status) =>
        Normalize(status) switch
        {
            PhotoSubmissionStatus.UnderReview
                or NewsSuggestionStatus.UnderReview
                or ArticleSubmissionStatus.UnderReview => "Under review",
            HelpRequestStatus.InProgress => "In progress",
            PhotoSubmissionStatus.NeedsInfo => "Needs info",
            ArticleSubmissionStatus.RequiresRevision => "Requires revision",
            ArticleSubmissionStatus.ApprovedForPublishing => "Approved for publishing",
            var known when !string.IsNullOrEmpty(known) => known,
            _ => "Unknown",
        };

    public static string TruncateUrl(string? url, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        if (url.Length <= maxLength)
        {
            return url;
        }

        return string.Concat(url.AsSpan(0, maxLength - 1), "…");
    }

    private static string Normalize(string? status) =>
        string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
}
