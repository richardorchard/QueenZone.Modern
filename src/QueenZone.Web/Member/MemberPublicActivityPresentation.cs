using QueenZone.Data;

namespace QueenZone.Web;

public static class MemberPublicActivityPresentation
{
    public const int PageSize = 20;

    public static MemberActivityViewModel ToViewModel(MemberPublicActivityItem item)
    {
        var summary = NewsArticleContent.ToPlainText(item.Summary ?? string.Empty);
        if (summary.Length > 220)
        {
            summary = summary[..217].TrimEnd() + "...";
        }

        return new MemberActivityViewModel(
            item.Type,
            item.Title,
            summary,
            item.PublishedAt,
            GetHref(item),
            item.AuthorId,
            item.AuthorDisplayName);
    }

    public static string GetHref(MemberPublicActivityItem item) => item.Type switch
    {
        MemberPublicActivityType.ForumPost when item.ParentId is int topicId =>
            $"{ForumRoutes.GetTopicCanonicalPath(topicId, item.Slug ?? item.Title)}#post-{item.ContentId}",
        MemberPublicActivityType.Article when !string.IsNullOrWhiteSpace(item.Slug) =>
            ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug),
        MemberPublicActivityType.News when item.ContentId is int newsId =>
            NewsRoutes.GetNewsDetailPath(newsId, item.Title, item.Slug),
        MemberPublicActivityType.Photo when !string.IsNullOrWhiteSpace(item.Category) =>
            $"/photography?category={Uri.EscapeDataString(item.Category)}",
        MemberPublicActivityType.Photo => "/photography",
        _ => "/",
    };
}

public sealed record MemberActivityViewModel(
    string Type,
    string Title,
    string Summary,
    DateTimeOffset PublishedAt,
    string Href,
    Guid? AuthorId = null,
    string? AuthorDisplayName = null)
{
    public string? AuthorProfileHref => AuthorId is Guid authorId ? $"/members/{authorId}" : null;
}
