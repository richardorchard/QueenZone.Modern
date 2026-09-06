namespace QueenZone.Data;

/// <summary>
/// Dev/sample-data implementation: scans every seeded category/topic/post through
/// <see cref="IForumRepository"/>. Fine for the small sample dataset; production reads use
/// <see cref="EfForumArchiveAuthorRepository"/>'s dedicated index instead of a full scan.
/// </summary>
public sealed class InMemoryForumArchiveAuthorRepository(IForumRepository forumRepository)
    : IForumArchiveAuthorRepository
{
    public async Task<ForumArchiveAuthorSummary?> GetSummaryAsync(
        int legacyUserId,
        CancellationToken cancellationToken = default)
    {
        var posts = await GetAllPostsByAuthorAsync(legacyUserId, cancellationToken);
        if (posts.Count == 0)
        {
            return null;
        }

        var latest = posts.OrderByDescending(entry => entry.Post.PostedAt).First().Post;
        return new ForumArchiveAuthorSummary(legacyUserId, latest.AuthorUsername, latest.AuthorMemberSince, posts.Count);
    }

    public async Task<MemberPublicActivityPage> GetPostsPageAsync(
        int legacyUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var ordered = (await GetAllPostsByAuthorAsync(legacyUserId, cancellationToken))
            .OrderByDescending(entry => entry.Post.PostedAt)
            .ToList();

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new MemberPublicActivityItem(
                MemberPublicActivityType.ForumPost,
                entry.ThreadTitle,
                entry.Post.Body,
                new DateTimeOffset(DateTime.SpecifyKind(entry.Post.PostedAt, DateTimeKind.Utc)),
                entry.Post.Id,
                entry.TopicId,
                NewsSlug.Slugify(entry.ThreadTitle),
                AuthorDisplayName: entry.Post.AuthorUsername))
            .ToList();

        return new MemberPublicActivityPage(items, ordered.Count, page, pageSize);
    }

    private async Task<List<(ForumPostItem Post, string ThreadTitle, int TopicId)>> GetAllPostsByAuthorAsync(
        int legacyUserId,
        CancellationToken cancellationToken)
    {
        var results = new List<(ForumPostItem Post, string ThreadTitle, int TopicId)>();
        var categories = await forumRepository.GetCategoriesAsync(cancellationToken);
        foreach (var category in categories)
        {
            var topicsPage = await forumRepository.GetCategoryTopicsPageAsync(
                category.Id,
                page: 1,
                pageSize: int.MaxValue,
                cancellationToken);
            foreach (var topic in topicsPage.Topics)
            {
                var postsPage = await forumRepository.GetTopicPostsPageAsync(
                    topic.Id,
                    page: 1,
                    pageSize: int.MaxValue,
                    cancellationToken);
                if (postsPage is null)
                {
                    continue;
                }

                results.AddRange(postsPage.Posts
                    .Where(post => post.AuthorLegacyUserId == legacyUserId)
                    .Select(post => (post, postsPage.Header.Title, topic.Id)));
            }
        }

        return results;
    }
}
