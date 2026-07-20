using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class CommunityArticleRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public CommunityArticleRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    // -------------------------------------------------------------------------
    // InMemoryArticleRepository unit tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPageAsync_ReturnsOnlyPublishedArticles()
    {
        var repo = new InMemoryArticleRepository(new StubSubmissionRepo(
        [
            Published("published-slug", "Published Title", DateTimeOffset.UtcNow.AddDays(-1)),
        ]));

        var result = await repo.GetPageAsync(1, 10);

        Assert.Single(result);
        Assert.Equal("published-slug", result[0].Slug);
    }

    [Fact]
    public async Task GetPageAsync_RespectsTagFilter()
    {
        var repo = new InMemoryArticleRepository(new StubSubmissionRepo(
        [
            Published("article-a", "Article A", DateTimeOffset.UtcNow.AddDays(-2), tags: "queen,freddie"),
            Published("article-b", "Article B", DateTimeOffset.UtcNow.AddDays(-1), tags: "roger,john"),
            Published("article-c", "Article C", DateTimeOffset.UtcNow, tags: "queen,brian"),
        ]));

        var result = await repo.GetPageAsync(1, 10, tag: "queen");

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.True(
            ("," + item.Tags + ",").Contains(",queen,", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNullWhenNoPublishedArticlesExist()
    {
        var repo = new InMemoryArticleRepository(new StubSubmissionRepo([]));

        var result = await repo.GetBySlugAsync("any-slug");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsPublishedArticle()
    {
        var repo = new InMemoryArticleRepository(new StubSubmissionRepo(
        [
            Published("my-article", "My Article", DateTimeOffset.UtcNow),
        ]));

        var result = await repo.GetBySlugAsync("my-article");

        Assert.NotNull(result);
        Assert.Equal("my-article", result.Slug);
        Assert.Equal("My Article", result.Title);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsPublishedCount()
    {
        var repo = new InMemoryArticleRepository(new StubSubmissionRepo(
        [
            Published("pub-1", "Published 1", DateTimeOffset.UtcNow.AddDays(-1)),
            Published("pub-2", "Published 2", DateTimeOffset.UtcNow),
        ]));

        var count = await repo.GetCountAsync();

        Assert.Equal(2, count);
    }

    // -------------------------------------------------------------------------
    // Web integration tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_Articles_Returns200_WithPublishedCommunityArticle()
    {
        var client = WithRepo([Published("test-community-slug", "Community Test Article", DateTimeOffset.UtcNow.AddDays(-1))]).CreateClient();

        var body = await client.GetStringAsync("/articles");

        Assert.Contains("Community Test Article", body);
    }

    [Fact]
    public async Task Get_CommunityDetail_Returns200_ForPublishedArticle()
    {
        var client = WithRepo([Published("my-published-article", "My Published Article", DateTimeOffset.UtcNow.AddDays(-1))]).CreateClient();

        var response = await client.GetAsync("/articles/my-published-article");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("My Published Article", body);
    }

    [Fact]
    public async Task Get_CommunityDetail_Returns404_ForUnknownSlug()
    {
        var client = WithRepo([]).CreateClient();

        var response = await client.GetAsync("/articles/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_CommunityDetail_Returns404_WhenSlugNotInPublishedSet()
    {
        var client = WithRepo([Published("different-slug", "Different Article", DateTimeOffset.UtcNow)]).CreateClient();

        var response = await client.GetAsync("/articles/draft-only-slug");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ArticlesFeed_Returns200_WithRssContent()
    {
        var client = WithRepo([Published("rss-test-article", "RSS Test Article", DateTimeOffset.UtcNow.AddDays(-1))]).CreateClient();

        var response = await client.GetAsync("/articles/feed.rss");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<rss", body);
        Assert.Contains("RSS Test Article", body);
        Assert.Contains("/articles/rss-test-article", body);
    }

    [Fact]
    public async Task Get_Articles_TagFilter_ShowsOnlyMatchingArticles()
    {
        var client = WithRepo(
        [
            Published("tagged-article", "Tagged Article", DateTimeOffset.UtcNow.AddDays(-2), tags: "queen,freddie"),
            Published("other-article", "Other Article", DateTimeOffset.UtcNow.AddDays(-1), tags: "roger"),
        ]).CreateClient();

        var body = await client.GetStringAsync("/articles?tag=queen");

        Assert.Contains("Tagged Article", body);
        Assert.DoesNotContain("Other Article", body);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private WebApplicationFactory<Program> WithRepo(IEnumerable<PublishedArticleSubmission> seed) =>
        factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddSingleton<IArticleRepository>(new StubArticleRepo(seed))));

    private static PublishedArticleSubmission Published(
        string slug, string title, DateTimeOffset publishedAt,
        string? tags = null) =>
        new(
            Guid.NewGuid(),
            title,
            slug,
            "Test excerpt.",
            "<p>Body text content.</p>",
            null,
            tags,
            publishedAt,
            "Test Author",
            100);

    // Stub IArticleSubmissionRepository — only GetPublishedAsync is used by InMemoryArticleRepository
    private sealed class StubSubmissionRepo(IEnumerable<PublishedArticleSubmission> published) : IArticleSubmissionRepository
    {
        private readonly IReadOnlyList<PublishedArticleSubmission> items = [.. published];

        public Task<IReadOnlyList<PublishedArticleSubmission>> GetPublishedAsync(CancellationToken ct = default) =>
            Task.FromResult(items);

        public Task<ArticleSubmission> UpsertDraftAsync(ArticleSubmissionDraft d, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<ArticleSubmission?> SubmitForReviewAsync(Guid id, Guid m, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ArticleSubmission>> GetDraftsForMemberAsync(Guid m, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ArticleSubmissionListItem>> GetPendingAsync(int p, int s, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<ArticleSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<ArticleSubmission?> UpdateStatusAsync(Guid id, string status, string? re,
            string? n, string? rr, string? sl = null, string? ex = null, string? t = null, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    // Minimal IArticleRepository stub for web integration tests
    private sealed class StubArticleRepo(IEnumerable<PublishedArticleSubmission> seed) : IArticleRepository
    {
        private readonly List<PublishedArticleSubmission> items = [.. seed];

        private static bool HasTag(string? tags, string tag) =>
            !string.IsNullOrWhiteSpace(tags) &&
            ("," + tags + ",").Contains("," + tag + ",", StringComparison.OrdinalIgnoreCase);

        public Task<int> GetCountAsync(string? tag = null, CancellationToken ct = default) =>
            Task.FromResult(string.IsNullOrWhiteSpace(tag) ? items.Count : items.Count(a => HasTag(a.Tags, tag)));

        public Task<IReadOnlyList<PublishedArticleSubmission>> GetPageAsync(
            int page, int pageSize, string? tag = null, CancellationToken ct = default)
        {
            IReadOnlyList<PublishedArticleSubmission> result = (string.IsNullOrWhiteSpace(tag)
                ? items : items.Where(a => HasTag(a.Tags, tag)))
                .Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(result);
        }

        public Task<PublishedArticleSubmission?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(items.FirstOrDefault(a => string.Equals(a.Slug, slug, StringComparison.OrdinalIgnoreCase)));

        public Task<(PublishedArticleSubmission? Previous, PublishedArticleSubmission? Next)> GetAdjacentAsync(
            DateTimeOffset publishedAt, CancellationToken ct = default) =>
            Task.FromResult<(PublishedArticleSubmission?, PublishedArticleSubmission?)>((null, null));

        public Task<IReadOnlyList<PublishedArticleSubmission>> GetSitemapEntriesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PublishedArticleSubmission>>(items);
    }
}
