using Microsoft.Extensions.Logging;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web;

public sealed class NewsForumTopicService(
    IForumWriteRepository forumWriteRepository,
    IForumRepository forumRepository,
    ForumPostWriteService forumPostWrite,
    IMemberAccountRepository memberAccounts,
    IAdminNewsRepository adminNews,
    TimeProvider timeProvider,
    ILogger<NewsForumTopicService> logger) : INewsForumTopicService
{
    public async Task EnsureTopicOnFirstPublishAsync(
        AdminNewsArticle article,
        CancellationToken cancellationToken = default)
    {
        if (article.ForumTopicId is not null)
        {
            return;
        }

        var latest = await adminNews.GetByIdAsync(article.Id, cancellationToken);
        if (latest?.ForumTopicId is not null)
        {
            return;
        }

        var categoryId = await forumWriteRepository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName,
            cancellationToken);
        var category = await forumRepository.GetCategoryByIdAsync(categoryId, cancellationToken);
        if (category is null || NewsForumDiscussion.IsTheMusic(category.Name))
        {
            throw new InvalidOperationException("News forum topic must not use The Music category.");
        }

        var author = await EnsureSystemMemberAsync(cancellationToken);
        var title = ClampTitle(article.Title, article.Id);
        var body = BuildOpeningPost(article);
        var outcome = await forumPostWrite.CreateTopicAsync(
            author.Id,
            NewsForumDiscussion.SystemMemberDisplayName,
            categoryId,
            title,
            body,
            attachments: null,
            poll: null,
            cancellationToken,
            trustedSystemAuthor: true);
        if (!outcome.Succeeded)
        {
            logger.LogWarning(
                "News forum topic create failed after news publish {NewsId}: {Status}",
                article.Id,
                outcome.Status);
            return;
        }

        await adminNews.TrySetForumTopicIdAsync(article.Id, outcome.TopicId, cancellationToken);
    }

    internal static string BuildOpeningPost(AdminNewsArticle article)
    {
        var excerpt = NewsForumDiscussion.TruncatePlain(
            article.Excerpt,
            NewsForumDiscussion.OpeningExcerptMaxLength);
        var path = NewsRoutes.GetNewsDetailPath(
            article.Id,
            article.Title,
            string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug);
        var url = NewsForumDiscussion.PublicArticleOrigin + path;
        return string.IsNullOrWhiteSpace(excerpt) ? url : excerpt + "\n\n" + url;
    }

    internal static string ClampTitle(string title, int articleId)
    {
        var trimmed = title.Trim();
        if (trimmed.Length > ForumPostWriteService.SubjectMaxLength)
        {
            trimmed = trimmed[..ForumPostWriteService.SubjectMaxLength].Trim();
        }

        return trimmed.Length < ForumPostWriteService.SubjectMinLength
            ? $"News article {articleId}"
            : trimmed;
    }

    private async Task<MemberAccount> EnsureSystemMemberAsync(CancellationToken cancellationToken)
    {
        var existing = await memberAccounts.FindByEmailAsync(
            NewsForumDiscussion.SystemMemberEmail,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        return await memberAccounts.CreateAsync(
            new MemberAccount
            {
                Id = Guid.NewGuid(),
                Email = NewsForumDiscussion.SystemMemberEmail,
                DisplayName = NewsForumDiscussion.SystemMemberDisplayName,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            },
            cancellationToken);
    }
}
