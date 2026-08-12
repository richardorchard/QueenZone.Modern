using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

public sealed class EfMemberPublicActivityRepository(QueenZoneDbContext dbContext)
    : IMemberPublicActivityRepository
{
    public async Task<MemberPublicActivityPage> GetPageAsync(
        Guid memberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var take = (int)Math.Min((long)page * pageSize, int.MaxValue);
        var isSqlite = dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        var forumRows = await dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorMemberId == memberId && post.Thread != null && !post.IsHidden)
            .OrderByDescending(post => post.PostedAt)
            .Take(take)
            .Select(post => new
            {
                post.LegacyPostId,
                post.LegacyThreadTopicId,
                ThreadTitle = post.Thread!.Title,
                post.BodyHtml,
                post.PostedAt,
            })
            .ToListAsync(cancellationToken);

        var articleQuery = dbContext.ArticleSubmissions
            .AsNoTracking()
            .Where(article => article.AuthorMemberId == memberId
                && article.Status == ArticleSubmissionStatus.Published
                && article.PublishedAt != null)
            .Select(article => new
            {
                article.Title,
                article.Slug,
                article.Excerpt,
                PublishedAt = article.PublishedAt!.Value,
            });
        var articleRows = isSqlite
            ? await articleQuery.ToListAsync(cancellationToken)
            : await articleQuery.OrderByDescending(article => article.PublishedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

        var newsQuery = dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(news => news.SubmitterMemberId == memberId
                && news.Status == NewsSuggestionStatus.Promoted
                && news.PromotedNewsId != null)
            .Select(news => new
            {
                NewsId = news.PromotedNewsId!.Value,
                news.Title,
                news.Notes,
                PublishedAt = news.ReviewedAt ?? news.SubmittedAt,
            });
        var newsRows = isSqlite
            ? await newsQuery.ToListAsync(cancellationToken)
            : await newsQuery.OrderByDescending(news => news.PublishedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

        var photoQuery = dbContext.PhotoSubmissions
            .AsNoTracking()
            .Where(photo => photo.SubmitterMemberId == memberId
                && photo.Status == PhotoSubmissionStatus.Approved)
            .Select(photo => new
            {
                photo.Title,
                photo.Description,
                photo.ApprovedCategory,
                PublishedAt = photo.ReviewedAt ?? photo.SubmittedAt,
            });
        var photoRows = isSqlite
            ? await photoQuery.ToListAsync(cancellationToken)
            : await photoQuery.OrderByDescending(photo => photo.PublishedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

        var totalCount = await dbContext.ModernForumPosts.CountAsync(
                post => post.AuthorMemberId == memberId && post.Thread != null && !post.IsHidden,
                cancellationToken)
            + await dbContext.ArticleSubmissions.CountAsync(
                article => article.AuthorMemberId == memberId
                    && article.Status == ArticleSubmissionStatus.Published
                    && article.PublishedAt != null,
                cancellationToken)
            + await dbContext.NewsSuggestions.CountAsync(
                news => news.SubmitterMemberId == memberId
                    && news.Status == NewsSuggestionStatus.Promoted
                    && news.PromotedNewsId != null,
                cancellationToken)
            + await dbContext.PhotoSubmissions.CountAsync(
                photo => photo.SubmitterMemberId == memberId
                    && photo.Status == PhotoSubmissionStatus.Approved,
                cancellationToken);

        var items = forumRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.ForumPost,
                row.ThreadTitle,
                row.BodyHtml,
                ToOffset(row.PostedAt),
                row.LegacyPostId,
                row.LegacyThreadTopicId,
                NewsSlug.Slugify(row.ThreadTitle)))
            .Concat(articleRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.Article,
                row.Title,
                row.Excerpt,
                row.PublishedAt,
                Slug: row.Slug)))
            .Concat(newsRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.News,
                string.IsNullOrWhiteSpace(row.Title) ? "News contribution" : row.Title,
                row.Notes,
                row.PublishedAt,
                ContentId: row.NewsId,
                Slug: NewsSlug.Slugify(row.Title ?? "news"))))
            .Concat(photoRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.Photo,
                row.Title,
                row.Description,
                row.PublishedAt,
                Category: row.ApprovedCategory)))
            .OrderByDescending(item => item.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new MemberPublicActivityPage(items, totalCount, page, pageSize);
    }

    private static DateTimeOffset ToOffset(DateTime? value) =>
        new(DateTime.SpecifyKind(value ?? DateTime.MinValue, DateTimeKind.Utc));
}
