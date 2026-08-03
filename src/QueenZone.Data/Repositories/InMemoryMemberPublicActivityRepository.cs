namespace QueenZone.Data;

public sealed class InMemoryMemberPublicActivityRepository(
    InMemoryForumWriteRepository forumWriteRepository,
    IArticleSubmissionRepository articleSubmissionRepository,
    IPhotoSubmissionRepository photoSubmissionRepository,
    INewsSuggestionRepository newsSuggestionRepository) : IMemberPublicActivityRepository
{
    public async Task<MemberPublicActivityPage> GetPageAsync(
        Guid memberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var forumItems = forumWriteRepository.GetCreatedThreads()
            .SelectMany(thread => forumWriteRepository.GetPostsForTopic(thread.TopicId)
                .Where(post => post.MemberId == memberId)
                .Select(post => new MemberPublicActivityItem(
                    MemberPublicActivityType.ForumPost,
                    thread.Subject,
                    post.Body,
                    post.CreatedAt,
                    post.PostId,
                    thread.TopicId,
                    NewsSlug.Slugify(thread.Subject))));

        var articleItems = (await articleSubmissionRepository.GetPublishedAsync(cancellationToken))
            .Where(article => article.AuthorMemberId == memberId)
            .Select(article => new MemberPublicActivityItem(
                MemberPublicActivityType.Article,
                article.Title,
                article.Excerpt,
                article.PublishedAt,
                Slug: article.Slug));

        var photoPage = await photoSubmissionRepository.GetBySubmitterAsync(
            memberId,
            pageSize: int.MaxValue,
            cancellationToken: cancellationToken);
        var photoItems = photoPage.Items
            .Where(photo => photo.Status == PhotoSubmissionStatus.Approved)
            .Select(photo => new MemberPublicActivityItem(
                MemberPublicActivityType.Photo,
                photo.Title,
                photo.Description,
                photo.ReviewedAt ?? photo.SubmittedAt,
                Category: photo.ApprovedCategory));

        var newsPage = await newsSuggestionRepository.GetBySubmitterAsync(
            memberId,
            pageSize: int.MaxValue,
            cancellationToken: cancellationToken);
        var newsItems = newsPage.Items
            .Where(news => news.Status == NewsSuggestionStatus.Promoted && news.PromotedNewsId is not null)
            .Select(news => new MemberPublicActivityItem(
                MemberPublicActivityType.News,
                string.IsNullOrWhiteSpace(news.Title) ? "News contribution" : news.Title,
                news.Notes,
                news.ReviewedAt ?? news.SubmittedAt,
                ContentId: news.PromotedNewsId,
                Slug: NewsSlug.Slugify(news.Title ?? "news")));

        var all = forumItems
            .Concat(articleItems)
            .Concat(photoItems)
            .Concat(newsItems)
            .OrderByDescending(item => item.PublishedAt)
            .ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new MemberPublicActivityPage(items, all.Count, page, pageSize);
    }
}
