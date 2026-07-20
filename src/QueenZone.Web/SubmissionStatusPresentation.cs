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
                or ArticleSubmissionStatus.Submitted => "pending",
            PhotoSubmissionStatus.UnderReview
                or NewsSuggestionStatus.UnderReview
                or ArticleSubmissionStatus.UnderReview
                or ArticleSubmissionStatus.ApprovedForPublishing => "review",
            PhotoSubmissionStatus.NeedsInfo
                or ArticleSubmissionStatus.RequiresRevision => "attention",
            PhotoSubmissionStatus.Approved
                or NewsSuggestionStatus.Promoted
                or ArticleSubmissionStatus.Published => "success",
            PhotoSubmissionStatus.Rejected
                or NewsSuggestionStatus.Rejected
                or NewsSuggestionStatus.Duplicate
                or ArticleSubmissionStatus.Rejected => "danger",
            _ => "neutral",
        };

    public static string GetLabel(string? status) =>
        Normalize(status) switch
        {
            PhotoSubmissionStatus.UnderReview
                or NewsSuggestionStatus.UnderReview
                or ArticleSubmissionStatus.UnderReview => "Under review",
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
