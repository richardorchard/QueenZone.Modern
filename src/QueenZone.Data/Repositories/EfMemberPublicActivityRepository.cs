using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

public sealed class EfMemberPublicActivityRepository(QueenZoneDbContext dbContext)
    : IMemberPublicActivityRepository
{
    public Task<MemberPublicActivityPage> GetPageAsync(
        Guid memberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetFeedPageAsync([memberId], page, pageSize, cancellationToken);

    public async Task<MemberPublicActivityPage> GetFeedPageAsync(
        IReadOnlyCollection<Guid> memberIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var authorIds = memberIds.Distinct().ToList();
        if (authorIds.Count == 0)
        {
            return new MemberPublicActivityPage([], 0, page, pageSize);
        }

        var take = (int)Math.Min((long)page * pageSize, int.MaxValue);
        var isSqlite = dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        var forumRows = await dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorMemberId != null
                && authorIds.Contains(post.AuthorMemberId.Value)
                && post.Thread != null
                && !post.IsHidden)
            .OrderByDescending(post => post.PostedAt)
            .Take(take)
            .Select(post => new
            {
                post.LegacyPostId,
                post.LegacyThreadTopicId,
                ThreadTitle = post.Thread!.Title,
                post.BodyHtml,
                post.PostedAt,
                AuthorId = post.AuthorMemberId!.Value,
                post.AuthorDisplayName,
            })
            .ToListAsync(cancellationToken);

        var articleQuery = dbContext.ArticleSubmissions
            .AsNoTracking()
            .Where(article => authorIds.Contains(article.AuthorMemberId)
                && article.Status == ArticleSubmissionStatus.Published
                && article.PublishedAt != null)
            .Select(article => new
            {
                article.Title,
                article.Slug,
                article.Excerpt,
                PublishedAt = article.PublishedAt!.Value,
                article.AuthorMemberId,
            });
        var articleRows = isSqlite
            ? await articleQuery.ToListAsync(cancellationToken)
            : await articleQuery.OrderByDescending(article => article.PublishedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

        var newsQuery = dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(news => authorIds.Contains(news.SubmitterMemberId)
                && news.Status == NewsSuggestionStatus.Promoted
                && news.PromotedNewsId != null)
            .Select(news => new
            {
                NewsId = news.PromotedNewsId!.Value,
                news.Title,
                news.Notes,
                PublishedAt = news.ReviewedAt ?? news.SubmittedAt,
                news.SubmitterMemberId,
            });
        var newsRows = isSqlite
            ? await newsQuery.ToListAsync(cancellationToken)
            : await newsQuery.OrderByDescending(news => news.PublishedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

        var photoQuery = dbContext.PhotoSubmissions
            .AsNoTracking()
            .Where(photo => authorIds.Contains(photo.SubmitterMemberId)
                && photo.Status == PhotoSubmissionStatus.Approved)
            .Select(photo => new
            {
                photo.Title,
                photo.Description,
                photo.ApprovedCategory,
                PublishedAt = photo.ReviewedAt ?? photo.SubmittedAt,
                photo.SubmitterMemberId,
            });
        var photoRows = isSqlite
            ? await photoQuery.ToListAsync(cancellationToken)
            : await photoQuery.OrderByDescending(photo => photo.PublishedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

        var totalCount = await dbContext.ModernForumPosts.CountAsync(
                post => post.AuthorMemberId != null
                    && authorIds.Contains(post.AuthorMemberId.Value)
                    && post.Thread != null
                    && !post.IsHidden,
                cancellationToken)
            + await dbContext.ArticleSubmissions.CountAsync(
                article => authorIds.Contains(article.AuthorMemberId)
                    && article.Status == ArticleSubmissionStatus.Published
                    && article.PublishedAt != null,
                cancellationToken)
            + await dbContext.NewsSuggestions.CountAsync(
                news => authorIds.Contains(news.SubmitterMemberId)
                    && news.Status == NewsSuggestionStatus.Promoted
                    && news.PromotedNewsId != null,
                cancellationToken)
            + await dbContext.PhotoSubmissions.CountAsync(
                photo => authorIds.Contains(photo.SubmitterMemberId)
                    && photo.Status == PhotoSubmissionStatus.Approved,
                cancellationToken);

        var names = await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(member => authorIds.Contains(member.Id))
            .Select(member => new { member.Id, member.DisplayName })
            .ToDictionaryAsync(member => member.Id, member => member.DisplayName, cancellationToken);

        string? NameFor(Guid authorId, string? fallback = null) =>
            names.TryGetValue(authorId, out var displayName) ? displayName : fallback;

        var items = forumRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.ForumPost,
                row.ThreadTitle,
                row.BodyHtml,
                ToOffset(row.PostedAt),
                row.LegacyPostId,
                row.LegacyThreadTopicId,
                NewsSlug.Slugify(row.ThreadTitle),
                AuthorId: row.AuthorId,
                AuthorDisplayName: NameFor(row.AuthorId, row.AuthorDisplayName)))
            .Concat(articleRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.Article,
                row.Title,
                row.Excerpt,
                row.PublishedAt,
                Slug: row.Slug,
                AuthorId: row.AuthorMemberId,
                AuthorDisplayName: NameFor(row.AuthorMemberId))))
            .Concat(newsRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.News,
                string.IsNullOrWhiteSpace(row.Title) ? "News contribution" : row.Title,
                row.Notes,
                row.PublishedAt,
                ContentId: row.NewsId,
                Slug: NewsSlug.Slugify(row.Title ?? "news"),
                AuthorId: row.SubmitterMemberId,
                AuthorDisplayName: NameFor(row.SubmitterMemberId))))
            .Concat(photoRows.Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.Photo,
                row.Title,
                row.Description,
                row.PublishedAt,
                Category: row.ApprovedCategory,
                AuthorId: row.SubmitterMemberId,
                AuthorDisplayName: NameFor(row.SubmitterMemberId))))
            .OrderByDescending(item => item.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new MemberPublicActivityPage(items, totalCount, page, pageSize);
    }

    private static DateTimeOffset ToOffset(DateTime? value) =>
        new(DateTime.SpecifyKind(value ?? DateTime.MinValue, DateTimeKind.Utc));
}
