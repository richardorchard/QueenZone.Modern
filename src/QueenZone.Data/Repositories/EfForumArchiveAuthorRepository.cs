using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Backed by <c>IX_ModernForumPost_AuthorLegacyUserId_PostedAt</c> so paging through a prolific
/// legacy author's posts stays fast even though the underlying table holds the whole imported
/// archive (see <see cref="QueenZoneDbContext"/> and the matching migration).
/// </summary>
public sealed class EfForumArchiveAuthorRepository(QueenZoneDbContext dbContext) : IForumArchiveAuthorRepository
{
    public async Task<ForumArchiveAuthorSummary?> GetSummaryAsync(
        int legacyUserId,
        CancellationToken cancellationToken = default)
    {
        var latest = await dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorLegacyUserId == legacyUserId && !post.IsHidden)
            .OrderByDescending(post => post.PostedAt)
            .Select(post => new { post.AuthorDisplayName, post.AuthorJoinedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null)
        {
            return null;
        }

        var postCount = await dbContext.ModernForumPosts.CountAsync(
            post => post.AuthorLegacyUserId == legacyUserId && !post.IsHidden,
            cancellationToken);

        return new ForumArchiveAuthorSummary(legacyUserId, latest.AuthorDisplayName, latest.AuthorJoinedAt, postCount);
    }

    public async Task<MemberPublicActivityPage> GetPostsPageAsync(
        int legacyUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorLegacyUserId == legacyUserId && !post.IsHidden && post.Thread != null);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(post => post.PostedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(post => new
            {
                post.LegacyPostId,
                post.LegacyThreadTopicId,
                ThreadTitle = post.Thread!.Title,
                post.BodyHtml,
                post.PostedAt,
                post.AuthorDisplayName,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new MemberPublicActivityItem(
                MemberPublicActivityType.ForumPost,
                row.ThreadTitle,
                row.BodyHtml,
                ToOffset(row.PostedAt),
                row.LegacyPostId,
                row.LegacyThreadTopicId,
                NewsSlug.Slugify(row.ThreadTitle),
                AuthorDisplayName: row.AuthorDisplayName))
            .ToList();

        return new MemberPublicActivityPage(items, totalCount, page, pageSize);
    }

    private static DateTimeOffset ToOffset(DateTime? value) =>
        new(DateTime.SpecifyKind(value ?? DateTime.MinValue, DateTimeKind.Utc));
}
