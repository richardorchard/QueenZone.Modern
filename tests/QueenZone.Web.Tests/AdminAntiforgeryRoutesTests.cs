using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminAntiforgeryRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AdminAntiforgeryRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task News_create_without_antiforgery_token_returns_bad_request()
    {
        var store = new SharedNewsStore();
        var client = AdminHttpTestHelpers.CreateClient(
            CreateInMemoryFactory(store),
            AdminHttpTestHelpers.AdminEmail);

        var response = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Missing token article",
                ["excerpt"] = "Excerpt",
                ["body"] = "Body",
                ["publishedAt"] = "2026-06-14"
            },
            includeAntiforgeryToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task News_publish_without_antiforgery_token_returns_bad_request()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                1,
                "Draft",
                "draft",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                null,
                null,
                null)
        ]);

        var client = AdminHttpTestHelpers.CreateClient(
            CreateInMemoryFactory(store),
            AdminHttpTestHelpers.AdminEmail);

        var response = await AdminHttpTestHelpers.PostNewsActionAsync(
            client,
            "/admin/news/1/publish",
            includeAntiforgeryToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Discovery_promote_without_antiforgery_token_returns_bad_request()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);

        var client = AdminHttpTestHelpers.CreateClient(
            CreateInMemoryFactory(new SharedNewsStore(), discoveryRepository),
            AdminHttpTestHelpers.AdminEmail);

        var response = await AdminHttpTestHelpers.PostDiscoveryActionAsync(
            client,
            $"/admin/news-discovery/{candidateId}/promote",
            candidateId,
            includeAntiforgeryToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateInMemoryFactory(
        SharedNewsStore store,
        INewsDiscoveryRepository? discoveryRepository = null) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SharedNewsStore>();
                services.RemoveAll<INewsRepository>();
                services.RemoveAll<IAdminNewsRepository>();
                services.RemoveAll<INewsAuditRepository>();
                services.RemoveAll<INewsDiscoveryRepository>();
                services.RemoveAll<SharedNewsDiscoveryStore>();
                services.AddSingleton(store);
                services.AddSingleton<INewsRepository>(_ => new QueenZone.Data.InMemoryNewsRepository(store));
                services.AddSingleton<IAdminNewsRepository>(_ => new InMemoryAdminNewsRepository(store));
                services.AddSingleton<INewsAuditRepository>(_ => new InMemoryNewsAuditRepository(store));
                if (discoveryRepository is not null)
                {
                    services.AddSingleton(discoveryRepository);
                }
                else
                {
                    services.AddSingleton<SharedNewsDiscoveryStore>();
                    services.AddSingleton<INewsDiscoveryRepository, InMemoryNewsDiscoveryRepository>();
                }
            }));

    private static async Task<int> SeedDraftedCandidateAsync(InMemoryNewsDiscoveryRepository discoveryRepository)
    {
        var sourceId = await discoveryRepository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "antiforgery-source",
            "Antiforgery Source",
            "https://example.com/",
            null,
            NewsDiscoverySourceType.AllowlistedPage,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));

        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await discoveryRepository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://example.com/antiforgery",
            "Antiforgery candidate",
            discoveredAt,
            "Excerpt",
            discoveredAt));

        await discoveryRepository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.NeedsReview,
                RelevanceScore: 0.9m,
                ConfidenceScore: 0.8m));
        await discoveryRepository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted));
        await discoveryRepository.UpsertDraftAsync(
            candidateId,
            new NewsAgentDraftUpsert(
                "Draft title",
                "draft-title",
                "Excerpt",
                "Body",
                null,
                null,
                null,
                discoveredAt.Date,
                null));

        return candidateId;
    }
}
