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
        var posts = dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorLegacyUserId == legacyUserId && !post.IsHidden);

        // Keep identity and count in one SQL round trip. This route previously counted the same
        // author's posts here and again while loading the page, which doubled an expensive query
        // under crawler traffic.
        var summary = await posts
            .OrderByDescending(post => post.PostedAt)
            .ThenByDescending(post => post.Id)
            .Select(post => new
            {
                post.AuthorDisplayName,
                post.AuthorJoinedAt,
                PostCount = posts.Count(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (summary is null)
        {
            return null;
        }

        return new ForumArchiveAuthorSummary(
            legacyUserId,
            summary.AuthorDisplayName,
            summary.AuthorJoinedAt,
            summary.PostCount);
    }

    public async Task<MemberPublicActivityPage> GetPostsPageAsync(
        int legacyUserId,
        int page,
        int pageSize,
        int totalCount,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        totalCount = Math.Max(totalCount, 0);

        var query = dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorLegacyUserId == legacyUserId && !post.IsHidden && post.Thread != null);

        var rows = await query
            .OrderByDescending(post => post.PostedAt)
            .ThenByDescending(post => post.Id)
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
