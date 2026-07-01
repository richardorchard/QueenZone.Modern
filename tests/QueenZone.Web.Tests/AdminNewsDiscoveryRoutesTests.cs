using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin.NewsDiscovery;

namespace QueenZone.Web.Tests;

public sealed partial class AdminNewsDiscoveryRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public AdminNewsDiscoveryRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task AnonymousUserCannotAccessDiscoveryReviewQueue()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/admin/news-discovery");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedNonAdminCannotAccessDiscoveryReviewQueue()
    {
        var client = CreateClient("stranger@example.com");

        var response = await client.GetAsync("/admin/news-discovery");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedAdminCanReviewRejectAndPromoteWithoutPublishingPublicly()
    {
        var newsStore = new SharedNewsStore();
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, newsStore, discoveryStore);

        var queueBody = await client.GetStringAsync("/admin/news-discovery?status=Drafted");
        Assert.Contains("Discovery review candidate", queueBody);
        Assert.Contains("/admin/news-discovery/" + candidateId, queueBody);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Discovery review candidate", reviewBody);
        Assert.Contains("https://www.queenonline.com/news/review-candidate", reviewBody);
        Assert.Contains("Mentions Queen tour dates.", reviewBody);
        Assert.Contains("Discovery draft title", reviewBody);

        var publicBeforePromote = await client.GetStringAsync("/news");
        Assert.DoesNotContain("Discovery draft title", publicBeforePromote);

        var promoteResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, promoteResponse.StatusCode);
        var editPath = promoteResponse.Headers.Location!.OriginalString;
        Assert.Matches("/admin/news/\\d+/edit", editPath);

        var articleId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);
        var publicAfterPromote = await client.GetStringAsync("/news");
        Assert.DoesNotContain("Discovery draft title", publicAfterPromote);

        var previewBody = await client.GetStringAsync($"/admin/news/{articleId}/preview");
        Assert.Contains("Discovery draft title", previewBody);
        Assert.Contains("This draft is hidden from the public archive.", previewBody);

        var promotedCandidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(promotedCandidate);
        Assert.Equal(NewsCandidateStatus.PromotedToArticle, promotedCandidate.Status);
        Assert.Equal(articleId, promotedCandidate.PromotedNewsId);
    }

    [Fact]
    public async Task AuthorizedAdminCanRejectCandidate()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var rejectResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/reject", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, rejectResponse.StatusCode);

        var candidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.Rejected, candidate.Status);
    }

    private static async Task<int> SeedNeedsReviewCandidateAsync(InMemoryNewsDiscoveryRepository repository)
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/reject-candidate",
            "Reject me",
            discoveredAt,
            "Excerpt",
            discoveredAt));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));
        return candidateId;
    }

    private static async Task<int> SeedDraftedCandidateAsync(InMemoryNewsDiscoveryRepository repository)
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/review-candidate",
            "Discovery review candidate",
            discoveredAt,
            "Official dates announced for 2026.",
            discoveredAt));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.NeedsReview,
                RelevanceScore: 0.9m,
                ConfidenceScore: 0.88m));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted));
        var aiRunId = await repository.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.Triage,
            "openrouter",
            "openai/gpt-4.1-nano",
            "triage-v1",
            discoveredAt));
        await repository.CompleteAiRunAsync(aiRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded,
            100,
            50,
            0.0001m,
            """{"verdict":"relevant","rationale":"Mentions Queen tour dates.","entities":["Queen"]}""",
            null,
            discoveredAt));
        await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Discovery draft title",
            "discovery-draft-title",
            "Draft excerpt for review queue.",
            "Draft body for review queue.",
            "Source: Queen Online",
            "Official announcement.",
            "High confidence.",
            discoveredAt.Date,
            null));
        return candidateId;
    }

    private WebApplicationFactory<Program> CreateFactory(SharedNewsStore newsStore, SharedNewsDiscoveryStore discoveryStore) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SharedNewsStore>();
                services.RemoveAll<SharedNewsDiscoveryStore>();
                services.RemoveAll<INewsRepository>();
                services.RemoveAll<IAdminNewsRepository>();
                services.RemoveAll<INewsAuditRepository>();
                services.RemoveAll<INewsDiscoveryRepository>();
                services.AddSingleton(newsStore);
                services.AddSingleton(discoveryStore);
                services.AddSingleton<INewsRepository>(_ => new QueenZone.Data.InMemoryNewsRepository(newsStore));
                services.AddSingleton<IAdminNewsRepository>(_ => new InMemoryAdminNewsRepository(newsStore));
                services.AddSingleton<INewsAuditRepository>(_ => new InMemoryNewsAuditRepository(newsStore));
                services.AddSingleton<INewsDiscoveryRepository>(_ => new InMemoryNewsDiscoveryRepository(discoveryStore));
            }));

    private HttpClient CreateClient(string? email = null, SharedNewsStore? newsStore = null, SharedNewsDiscoveryStore? discoveryStore = null)
    {
        newsStore ??= new SharedNewsStore();
        discoveryStore ??= new SharedNewsDiscoveryStore();
        var appFactory = CreateFactory(newsStore, discoveryStore);
        return CreateClientFromFactory(appFactory, email);
    }

    private static HttpClient CreateClientFromFactory(WebApplicationFactory<Program> appFactory, string? email)
    {
        var client = appFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        }

        return client;
    }

    private static async Task<HttpResponseMessage> PostActionAsync(HttpClient client, string actionPath, int candidateId)
    {
        var reviewPage = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        var token = ExtractAntiforgeryToken(reviewPage);
        return await client.PostAsync(actionPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [AdminNewsDiscoveryPageModel.AntiforgeryTokenFieldName] = token
        }));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
