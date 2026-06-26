using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

public sealed class ForumSitemapBuilderTests
{
    [Fact]
    public async Task BuildFileAsync_UsesTopicPageOneCanonicalPathsOnly()
    {
        var builder = new ForumSitemapBuilder(new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats()));

        var entries = await builder.BuildFileAsync(1);

        Assert.NotNull(entries);
        Assert.Contains(
            entries,
            entry => entry.Path == "/forum/topic/1002/ranking-every-studio-album");
        Assert.DoesNotContain(entries, entry => entry.Path.Contains("/page/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetFileCountAsync_ReturnsZeroWhenNoTopics()
    {
        var builder = new ForumSitemapBuilder(new EmptyForumRepository());

        var fileCount = await builder.GetFileCountAsync();

        Assert.Equal(0, fileCount);
    }

    private sealed class EmptyForumRepository : IForumRepository
    {
        public Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ForumCategoryItem>>([]);

        public Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ForumCategoryItem?>(null);

        public Task<ForumCategoryTopicsPage> GetCategoryTopicsPageAsync(
            int forumId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ForumCategoryTopicsPage([], 0, page, pageSize));

        public Task<ForumTopicPostsPage?> GetTopicPostsPageAsync(
            int topicId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ForumTopicPostsPage?>(null);

        public Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ForumArchiveStats(0, 0, 0));

        public Task<int> GetTopicSitemapCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<ForumTopicSitemapItem>> GetTopicSitemapPageAsync(
            int offset,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ForumTopicSitemapItem>>([]);
    }
}