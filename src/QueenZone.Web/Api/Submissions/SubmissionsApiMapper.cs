using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Maps repository rows to the mobile submissions contract using the same
/// status labels, notes, and published links as <c>/account/my-submissions</c>.
/// </summary>
public static class SubmissionsApiMapper
{
    public static SubmissionStatusDto ToStatus(string status) =>
        new(
            status,
            SubmissionStatusPresentation.GetLabel(status),
            SubmissionStatusPresentation.GetModifier(status));

    public static PhotoSubmissionItemDto ToPhoto(PhotoSubmission item) =>
        new(
            item.Id,
            item.Title,
            item.SubmittedAt,
            ToStatus(item.Status),
            PhotoNotes(item),
            string.IsNullOrWhiteSpace(item.ThumbnailBlobPath)
                ? null
                : UgcProxyPaths.GetPath(BlobUploadContainers.Photos, item.ThumbnailBlobPath),
            item.PromotedPicId);

    public static IReadOnlyList<PhotoSubmissionItemDto> ToPhotos(IEnumerable<PhotoSubmission> items) =>
        items.Select(ToPhoto).ToList();

    public static NewsSuggestionItemDto ToNews(NewsSuggestion item, string? publishedPath) =>
        new(
            item.Id,
            item.Url,
            SubmissionStatusPresentation.TruncateUrl(item.Url),
            item.Title,
            item.SubmittedAt,
            ToStatus(item.Status),
            EmptyToNull(item.ReviewNotes),
            item.PromotedNewsId,
            publishedPath);

    public static ArticleSubmissionItemDto ToArticle(ArticleSubmission item)
    {
        var canEdit = item.Status is ArticleSubmissionStatus.Draft or ArticleSubmissionStatus.RequiresRevision;
        var published = item.Status == ArticleSubmissionStatus.Published;
        return new(
            item.Id,
            item.Title,
            item.SubmittedAt,
            ToStatus(item.Status),
            ArticleNotes(item),
            canEdit,
            canEdit ? $"/submit/article/{item.Id:D}" : null,
            published ? ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug) : null);
    }

    public static IReadOnlyList<ArticleSubmissionItemDto> ToArticles(IEnumerable<ArticleSubmission> items) =>
        items.Select(ToArticle).ToList();

    public static async Task<string?> ResolvePublishedNewsPathAsync(
        NewsSuggestion suggestion,
        INewsRepository newsRepository,
        CancellationToken cancellationToken)
    {
        if (suggestion.Status != NewsSuggestionStatus.Promoted
            || suggestion.PromotedNewsId is not int newsId)
        {
            return null;
        }

        var news = await newsRepository.GetByIdAsync(newsId, cancellationToken);
        if (news is not { IsPublished: true })
        {
            return null;
        }

        return NewsRoutes.GetNewsDetailPath(news);
    }

    public static string? PhotoNotes(PhotoSubmission item)
    {
        if (item.Status == PhotoSubmissionStatus.Rejected
            && !string.IsNullOrWhiteSpace(item.RejectionReason))
        {
            return item.RejectionReason.Trim();
        }

        return EmptyToNull(item.ReviewNotes);
    }

    public static string? ArticleNotes(ArticleSubmission item)
    {
        if (item.Status == ArticleSubmissionStatus.RequiresRevision
            && !string.IsNullOrWhiteSpace(item.ReviewNotes))
        {
            return item.ReviewNotes.Trim();
        }

        if (!string.IsNullOrWhiteSpace(item.RejectionReason))
        {
            return item.RejectionReason.Trim();
        }

        return EmptyToNull(item.ReviewNotes);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
