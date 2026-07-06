using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PublicOutputCacheTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PublicOutputCacheTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task RazorPagesAreNotServedFromOutputCache()
    {
        var repository = new CountingArticlesRepository();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IArticlesRepository>(repository);
            })).CreateClient();

        var first = await client.GetStringAsync("/articles");
        var callsAfterFirstRequest = repository.ArchivePageCallCount + repository.PublishedCountCallCount;
        var second = await client.GetStringAsync("/articles");

        Assert.Contains("Cached archive article", first);
        Assert.Equal(first, second);
        Assert.True(callsAfterFirstRequest > 0);
        Assert.True(repository.ArchivePageCallCount + repository.PublishedCountCallCount > callsAfterFirstRequest);
    }

    [Theory]
    [InlineData("GET", "/articles", true)]
    [InlineData("HEAD", "/articles", true)]
    [InlineData("POST", "/articles", false)]
    [InlineData("GET", "/admin/news", false)]
    [InlineData("GET", "/account/login", false)]
    [InlineData("GET", "/account/member-probe", false)]
    [InlineData("GET", "/health", false)]
    public void PublicReadOnlyPolicyIncludesOnlyAnonymousPublicGetAndHeadRoutes(
        string method,
        string path,
        bool expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;

        Assert.Equal(expected, PublicOutputCachePolicies.IsPublicReadOnlyRequest(context));
    }

    private sealed class CountingArticlesRepository : IArticlesRepository
    {
        private readonly ArticleItem article = new(
            7801,
            "Cached archive article",
            "Output cache test article.",
            "<p>Output cache test body.</p>",
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
            null,
            "Testing",
            true);

        public int ArchivePageCallCount { get; private set; }

        public int PublishedCountCallCount { get; private set; }

        public Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArticleItem>>([article]);

        public Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ArchivePageCallCount++;
            return Task.FromResult<IReadOnlyList<ArticleItem>>([article]);
        }

        public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            PublishedCountCallCount++;
            return Task.FromResult(1);
        }

        public Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArticleItem?>(id == article.Id ? article : null);

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>(
                [new SitemapContentEntry(article.Id, article.Title, article.PublishedAt)]);
    }
}
