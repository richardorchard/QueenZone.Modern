using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Centralized mapping from repository DTOs to stable public view models.
/// Call once at the Web edge; page models and Razor views should consume view models only.
/// </summary>
public static class PublicContentMapper
{
    public static NewsArchiveItem ToNewsArchiveItem(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.PublishedAt,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static IReadOnlyList<NewsArchiveItem> ToNewsArchiveItems(IEnumerable<NewsItem> items) =>
        items.Select(ToNewsArchiveItem).ToList();

    public static NewsDetailItem ToNewsDetailItem(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.Body,
            item.PublishedAt,
            item.SourceUrl,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static NewsDetailItem ToNewsDetailItem(AdminNewsArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl,
            NewsRoutes.GetNewsDetailPath(
                article.Id,
                article.Title,
                string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug));

    public static ArticleArchiveItem ToArticleArchiveItem(ArticleItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.PublishedAt,
            item.CategoryName,
            ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title));

    public static IReadOnlyList<ArticleArchiveItem> ToArticleArchiveItems(IEnumerable<ArticleItem> items) =>
        items.Select(ToArticleArchiveItem).ToList();

    public static ArticleDetailItem ToArticleDetailItem(ArticleItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.Body,
            item.PublishedAt,
            item.Source,
            item.CategoryName,
            ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title));

    public static ForumCategorySummary ToForumCategorySummary(ForumCategoryItem category) =>
        new(
            category.Id,
            category.Name,
            category.Description,
            category.PostCount,
            category.LastActivityAt,
            category.LatestThreadTitle,
            ForumRoutes.GetCategoryCanonicalPath(category.Id, category.Name));

    public static IReadOnlyList<ForumCategorySummary> ToForumCategorySummaries(
        IEnumerable<ForumCategoryItem> categories) =>
        categories.Select(ToForumCategorySummary).ToList();

    public static ForumThreadSummary ToForumThreadSummary(ForumTopicItem topic) =>
        new(
            topic.Id,
            topic.Title,
            topic.LastActivityAt,
            topic.AuthorUsername,
            topic.ReplyCount,
            topic.LastPostUsername,
            topic.IsSticky,
            ForumRoutes.GetTopicCanonicalPath(topic.Id, topic.Title));

    public static IReadOnlyList<ForumThreadSummary> ToForumThreadSummaries(
        IEnumerable<ForumTopicItem> topics) =>
        topics.Select(ToForumThreadSummary).ToList();

    public static ForumThreadHeader ToForumThreadHeader(ForumTopicHeader header) =>
        new(
            header.TopicId,
            header.Title.Trim(),
            header.ForumId,
            header.ForumName.Trim(),
            ForumRoutes.GetCategoryCanonicalPath(header.ForumId, header.ForumName),
            ForumRoutes.GetTopicCanonicalPath(header.TopicId, header.Title),
            header.HasPoll);

    public static ForumPostViewModel ToForumPostViewModel(ForumPostItem post) =>
        new(
            post.Id,
            post.Body,
            post.PostedAt,
            post.AuthorUsername,
            post.Signature,
            post.AuthorMemberSince,
            ToForumAttachments(post.Attachments),
            post.AuthorMemberId,
            post.EditedAt,
            post.EditCount);

    public static ForumPostViewModel WithEditState(
        ForumPostViewModel post,
        Guid? currentMemberId,
        bool isAdmin,
        int editWindowMinutes,
        DateTimeOffset utcNow)
    {
        var postedAt = new DateTimeOffset(DateTime.SpecifyKind(post.PostedAt, DateTimeKind.Utc));
        var canEdit = ForumPostEditRules.CanEdit(
            post.AuthorMemberId,
            currentMemberId,
            isAdmin,
            postedAt,
            editWindowMinutes,
            utcNow);
        var showEdited = ForumPostEditRules.ShowEditedIndicator(post.EditCount, post.EditedAt, postedAt);
        var editedLabel = showEdited && post.EditedAt is not null
            ? ForumPostEditRules.FormatEditedLabel(post.EditCount, post.EditedAt.Value, utcNow)
            : null;

        return post with { CanEdit = canEdit, EditedLabel = editedLabel };
    }

    public static IReadOnlyList<ForumPostViewModel> ToForumPostViewModels(
        IEnumerable<ForumPostItem> posts) =>
        posts.Select(ToForumPostViewModel).ToList();

    public static ForumIndexStats ToForumIndexStats(
        IReadOnlyList<ForumCategoryItem> categories,
        int threadCount) =>
        new(
            categories.Count,
            threadCount,
            categories.Sum(category => (long)category.PostCount));

    private static IReadOnlyList<ForumAttachmentViewModel> ToForumAttachments(
        IReadOnlyList<ForumPostAttachment>? attachments)
    {
        if (attachments is null || attachments.Count == 0)
        {
            return [];
        }

        return attachments
            .Select(attachment => new ForumAttachmentViewModel(
                attachment.FileName,
                attachment.Url,
                attachment.Extension,
                attachment.FormattedSize,
                attachment.IsImage,
                attachment.ThumbnailUrl))
            .ToList();
    }
}
