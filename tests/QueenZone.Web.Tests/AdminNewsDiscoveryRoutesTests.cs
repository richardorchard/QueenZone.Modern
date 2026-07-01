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
        var editBody = await client.GetStringAsync(editPath);
        Assert.Contains("Discovery provenance", editBody);
        Assert.Contains($"/admin/news-discovery/{candidateId}", editBody);

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

    [Fact]
    public async Task AuthorizedAdminCanIgnoreDuplicateCandidate()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var (firstId, secondId) = await SeedDuplicatePairAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var ignoreResponse = await PostActionAsync(client, $"/admin/news-discovery/{secondId}/ignoreduplicate", secondId);
        Assert.Equal(HttpStatusCode.Redirect, ignoreResponse.StatusCode);

        var ignored = await discoveryRepository.GetCandidateByIdAsync(secondId);
        Assert.NotNull(ignored);
        Assert.Equal(NewsCandidateStatus.IgnoredDuplicate, ignored.Status);
        Assert.Equal(firstId, ignored.DuplicateOfCandidateId);
    }

    [Fact]
    public async Task AuthorizedAdminCanPromoteNeedsReviewCandidateWithAttribution()
    {
        var newsStore = new SharedNewsStore();
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateWithDraftAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, newsStore, discoveryStore);

        var promoteResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, promoteResponse.StatusCode);

        var editPath = promoteResponse.Headers.Location!.OriginalString;
        var articleId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);
        var previewBody = await client.GetStringAsync($"/admin/news/{articleId}/preview");
        Assert.Contains("Needs-review draft title", previewBody);
        Assert.Contains("Source: Queen Online", previewBody);

        var candidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.PromotedToArticle, candidate.Status);
    }

    [Fact]
    public async Task AuthorizedAdminCanEditDraftAndMoveCandidateToDrafted()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateWithDraftAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var editPage = await client.GetStringAsync($"/admin/news-discovery/{candidateId}/edit-draft");
        Assert.Contains("Needs-review draft title", editPage);

        var saveResponse = await PostDraftEditAsync(
            client,
            candidateId,
            new Dictionary<string, string>
            {
                ["form.title"] = "Edited discovery draft",
                ["form.slug"] = "edited-discovery-draft",
                ["form.excerpt"] = "Edited excerpt.",
                ["form.body"] = "Edited body text.",
                ["form.attributionText"] = "Edited attribution.",
                ["form.sourceNotes"] = "Edited source notes.",
                ["form.confidenceNotes"] = "Edited confidence notes.",
                ["form.suggestedPublishAt"] = "2026-07-15"
            });
        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);
        Assert.Equal($"/admin/news-discovery/{candidateId}", saveResponse.Headers.Location!.OriginalString);

        var draft = await discoveryRepository.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(draft);
        Assert.Equal("Edited discovery draft", draft.ProposedTitle);

        var candidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.Drafted, candidate.Status);
    }

    [Fact]
    public async Task EditDraftValidationFailuresAreReturnedForInvalidForm()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateWithDraftAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var response = await PostDraftEditAsync(
            client,
            candidateId,
            new Dictionary<string, string>
            {
                ["form.title"] = "",
                ["form.excerpt"] = "",
                ["form.body"] = ""
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", body);
        Assert.Contains("Excerpt is required.", body);
        Assert.Contains("Body is required.", body);
    }

    [Fact]
    public async Task ReviewAndEditDraftReturnNotFoundForMissingCandidate()
    {
        var client = CreateClient(AdminEmail);

        var reviewResponse = await client.GetAsync("/admin/news-discovery/99999");
        Assert.Equal(HttpStatusCode.NotFound, reviewResponse.StatusCode);

        var editResponse = await client.GetAsync("/admin/news-discovery/99999/edit-draft");
        Assert.Equal(HttpStatusCode.NotFound, editResponse.StatusCode);
    }

    [Fact]
    public async Task RejectActionReturnsNotFoundForMissingCandidate()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);
        var reviewPage = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        var token = ExtractAntiforgeryToken(reviewPage);

        var response = await client.PostAsync(
            "/admin/news-discovery/99999/reject",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [AdminNewsDiscoveryPageModel.AntiforgeryTokenFieldName] = token
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RejectActionRedirectsWhenCandidateCannotBeRejectedAgain()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/reject", candidateId);
        var secondReject = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/reject", candidateId);

        Assert.Equal(HttpStatusCode.Redirect, secondReject.StatusCode);
        Assert.Equal($"/admin/news-discovery/{candidateId}", secondReject.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ReviewPageShowsDuplicateLinkWhenConfigured()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var (firstId, secondId) = await SeedDuplicatePairAsync(discoveryRepository);
        await discoveryRepository.TryUpdateCandidateStatusAsync(
            secondId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.IgnoredDuplicate,
                DuplicateOfCandidateId: firstId));
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var body = await client.GetStringAsync($"/admin/news-discovery/{secondId}");
        Assert.Contains($"Candidate #{firstId}", body);
        Assert.Contains("Original Queen story", body);
    }

    [Fact]
    public async Task IndexFiltersByTrustTierAndHasDraft()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var filteredBody = await client.GetStringAsync("/admin/news-discovery?trustTier=Primary&hasDraft=true");
        Assert.Contains("Discovery review candidate", filteredBody);
        Assert.Contains($"/admin/news-discovery/{candidateId}", filteredBody);

        var emptyBody = await client.GetStringAsync("/admin/news-discovery?trustTier=Secondary&hasDraft=true");
        Assert.Contains("No candidates match the current filters.", emptyBody);
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

    private static async Task<int> SeedNeedsReviewCandidateWithDraftAsync(InMemoryNewsDiscoveryRepository repository)
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
            "https://www.queenonline.com/news/needs-review-draft",
            "Needs review with draft",
            discoveredAt,
            "Excerpt",
            discoveredAt));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));
        await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Needs-review draft title",
            "needs-review-draft-title",
            "Needs-review excerpt.",
            "Needs-review body.",
            "Source: Queen Online",
            null,
            null,
            discoveredAt.Date,
            null));
        return candidateId;
    }

    private static async Task<(int FirstId, int SecondId)> SeedDuplicatePairAsync(InMemoryNewsDiscoveryRepository repository)
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
        var firstId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/original",
            "Original Queen story",
            discoveredAt,
            "Excerpt",
            discoveredAt));
        await repository.TryUpdateCandidateStatusAsync(
            firstId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));
        var secondId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/duplicate-story",
            "Original Queen story",
            discoveredAt.AddMinutes(5),
            "Duplicate excerpt.",
            discoveredAt.AddMinutes(5)));
        await repository.TryUpdateCandidateStatusAsync(
            secondId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));
        return (firstId, secondId);
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

    private static async Task<HttpResponseMessage> PostDraftEditAsync(
        HttpClient client,
        int candidateId,
        Dictionary<string, string> fields)
    {
        var formPage = await client.GetStringAsync($"/admin/news-discovery/{candidateId}/edit-draft");
        fields[AdminNewsDiscoveryPageModel.AntiforgeryTokenFieldName] = ExtractAntiforgeryToken(formPage);
        return await client.PostAsync($"/admin/news-discovery/{candidateId}/edit-draft", new FormUrlEncodedContent(fields));
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
