using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.NewsAgent;
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
    public async Task DeletePromotedArticle_clears_discovery_link()
    {
        var newsStore = new SharedNewsStore();
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, newsStore, discoveryStore);

        var promoteResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        var articleId = int.Parse(promoteResponse.Headers.Location!.OriginalString.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);

        var deleteResponse = await PostAdminNewsActionAsync(client, $"/admin/news/{articleId}/delete");
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var candidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Null(candidate.PromotedNewsId);
    }

    [Fact]
    public async Task AuthorizedAdminCanRegenerateDraftFromReviewPage()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/regeneratedraft", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Draft regenerated successfully", reviewBody);
        Assert.Contains("Queen announce 2026 tour", reviewBody);

        var draft = await discoveryRepository.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(draft);
        Assert.Equal("Queen announce 2026 tour", draft.ProposedTitle);
    }

    [Fact]
    public async Task RegenerateDraft_reports_error_when_openrouter_is_not_configured()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(
            AdminEmail,
            new SharedNewsStore(),
            discoveryStore,
            openRouterApiKey: null,
            aiClientEnabled: false);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/regeneratedraft", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Draft regeneration requires OpenRouter configuration", reviewBody);
    }

    [Fact]
    public async Task RegenerateDraft_reports_error_for_rejected_candidate()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateAsync(discoveryRepository);
        await discoveryRepository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Rejected));
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/regeneratedraft", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Only needs-review or drafted candidates can regenerate a draft", reviewBody);
    }

    [Fact]
    public async Task RegenerateDraft_reports_failure_when_ai_returns_invalid_json()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(
            AdminEmail,
            new SharedNewsStore(),
            discoveryStore,
            draftResponseJson: "{not-json");

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/regeneratedraft", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Draft regeneration failed", reviewBody);
    }

    [Fact]
    public async Task RegenerateDraft_reports_when_ai_returns_no_structured_content()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(
            AdminEmail,
            new SharedNewsStore(),
            discoveryStore,
            draftResponseJson: string.Empty);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/regeneratedraft", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Draft regeneration did not produce a new draft", reviewBody);
    }

    [Fact]
    public async Task RegenerateDraft_returns_not_found_for_missing_candidate()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore);
        var reviewPage = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        var token = ExtractAntiforgeryToken(reviewPage);

        var response = await client.PostAsync(
            "/admin/news-discovery/99999/regeneratedraft",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [AdminNewsDiscoveryPageModel.AntiforgeryTokenFieldName] = token
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
    public async Task Promote_with_overlong_title_shows_validation_error()
    {
        var newsStore = new SharedNewsStore();
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        await discoveryRepository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            new string('x', NewsValidation.MaxTitleLength + 1),
            "overlong-title",
            "Draft excerpt for review queue.",
            "Draft body for review queue.",
            null,
            null,
            null,
            DateTime.UtcNow.Date,
            null));
        var client = CreateClient(AdminEmail, newsStore, discoveryStore);

        var promoteResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, promoteResponse.StatusCode);
        Assert.Equal($"/admin/news-discovery/{candidateId}", promoteResponse.Headers.Location!.OriginalString);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains($"Title must be {NewsValidation.MaxTitleLength} characters or fewer.", reviewBody);
        Assert.Contains("admin-status--error", reviewBody);

        var candidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.Drafted, candidate.Status);
        Assert.Null(candidate.PromotedNewsId);
    }

    [Fact]
    public async Task Promote_with_overlong_source_url_shows_validation_error()
    {
        var newsStore = new SharedNewsStore();
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var longUrl = "https://www.queenonline.com/news/" + new string('a', NewsValidation.MaxSourceUrlLength);
        var sourceId = await discoveryRepository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "long-url-source",
            "Long URL Source",
            "https://example.com/",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var discoveredAt = DateTime.UtcNow;
        var candidateId = await discoveryRepository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            longUrl,
            "Long URL candidate",
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
        await discoveryRepository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Valid title",
            "valid-title",
            "Draft excerpt",
            "Draft body",
            null,
            null,
            null,
            discoveredAt.Date,
            null));

        var client = CreateClient(AdminEmail, newsStore, discoveryStore);
        var promoteResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, promoteResponse.StatusCode);
        Assert.Equal($"/admin/news-discovery/{candidateId}", promoteResponse.Headers.Location!.OriginalString);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains($"Source URL must be {NewsValidation.MaxSourceUrlLength} characters or fewer.", reviewBody);
        Assert.Contains("admin-status--error", reviewBody);
    }

    [Fact]
    public async Task Promote_with_overlong_ai_slug_caps_slug_before_creating_draft()
    {
        var newsStore = new SharedNewsStore();
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        await discoveryRepository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Draft title with verbose generated slug",
            new string('a', NewsSlug.MaxLength + 50),
            "Draft excerpt for review queue.",
            "Draft body for review queue.",
            null,
            null,
            null,
            DateTime.UtcNow.Date,
            null));
        var client = CreateClient(AdminEmail, newsStore, discoveryStore);

        var promoteResponse = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);

        Assert.Equal(HttpStatusCode.Redirect, promoteResponse.StatusCode);
        var editPath = promoteResponse.Headers.Location!.OriginalString;
        Assert.Matches("/admin/news/\\d+/edit", editPath);

        var articleId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);
        var article = newsStore.GetArticle(articleId);
        Assert.NotNull(article);
        Assert.Equal(NewsSlug.MaxLength, article.Slug!.Length);
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
    public async Task RejectActionShowsErrorWhenStatusUpdateFails()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var inner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateAsync(inner);
        var discoveryRepository = new ConfigurableNewsDiscoveryRepository(inner)
        {
            TryUpdateCandidateStatusHandler = (id, update, ct) =>
                update.Status == NewsCandidateStatus.Rejected
                    ? Task.FromResult(false)
                    : inner.TryUpdateCandidateStatusAsync(id, update, ct)
        };
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/reject", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("could not be marked as rejected", reviewBody);
    }

    [Fact]
    public async Task IgnoreDuplicateActionShowsErrorWhenAlreadyIgnored()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var (_, secondId) = await SeedDuplicatePairAsync(discoveryRepository);
        await discoveryRepository.TryUpdateCandidateStatusAsync(
            secondId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.IgnoredDuplicate,
                DuplicateOfCandidateId: 1));
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{secondId}/ignoreduplicate", secondId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{secondId}");
        Assert.Contains("already been ignored as a duplicate", reviewBody);
    }

    [Fact]
    public async Task IgnoreDuplicateActionShowsErrorWhenStatusUpdateFails()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var inner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateAsync(inner);
        var discoveryRepository = new ConfigurableNewsDiscoveryRepository(inner)
        {
            TryUpdateCandidateStatusHandler = (id, update, ct) =>
                update.Status == NewsCandidateStatus.IgnoredDuplicate
                    ? Task.FromResult(false)
                    : inner.TryUpdateCandidateStatusAsync(id, update, ct)
        };
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/ignoreduplicate", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("could not be marked as a duplicate", reviewBody);
    }

    [Fact]
    public async Task PromoteActionShowsErrorWhenDraftIsMissing()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var inner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(inner);
        var discoveryRepository = new ConfigurableNewsDiscoveryRepository(inner)
        {
            GetDraftByCandidateIdHandler = (_, _) => Task.FromResult<NewsAgentDraft?>(null)
        };
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Generate or save a draft before promoting", reviewBody);
    }

    [Fact]
    public async Task PromoteActionShowsErrorWhenCandidateIsNotDrafted()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDiscoveredCandidateWithDraftAsync(discoveryRepository);
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Only drafted candidates can be promoted", reviewBody);
    }

    [Fact]
    public async Task PromoteActionShowsErrorWhenDraftAcknowledgementFails()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var inner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedNeedsReviewCandidateWithDraftAsync(inner);
        var discoveryRepository = new ConfigurableNewsDiscoveryRepository(inner)
        {
            TryUpdateCandidateStatusHandler = (id, update, ct) =>
                update.Status == NewsCandidateStatus.Drafted
                    ? Task.FromResult(false)
                    : inner.TryUpdateCandidateStatusAsync(id, update, ct)
        };
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("could not be marked as drafted before promotion", reviewBody);
    }

    [Fact]
    public async Task PromoteActionShowsErrorWhenCandidateStatusUpdateFails()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var inner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(inner);
        var discoveryRepository = new ConfigurableNewsDiscoveryRepository(inner)
        {
            TryUpdateCandidateStatusHandler = (id, update, ct) =>
                update.Status == NewsCandidateStatus.PromotedToArticle
                    ? Task.FromResult(false)
                    : inner.TryUpdateCandidateStatusAsync(id, update, ct)
        };
        var client = CreateClient(AdminEmail, new SharedNewsStore(), discoveryStore, discoveryRepository: discoveryRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Promotion failed while updating the discovery candidate", reviewBody);
    }

    [Fact]
    public async Task PromoteActionShowsErrorWhenCreateDraftFails()
    {
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryRepository = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var candidateId = await SeedDraftedCandidateAsync(discoveryRepository);
        var newsStore = new SharedNewsStore();
        var adminRepository = new FailingCreateAdminNewsRepository(
            new InMemoryAdminNewsRepository(newsStore),
            new InvalidOperationException("Simulated create failure."));
        var client = CreateClient(
            AdminEmail,
            newsStore,
            discoveryStore,
            discoveryRepository: discoveryRepository,
            adminNewsRepository: adminRepository);

        var response = await PostActionAsync(client, $"/admin/news-discovery/{candidateId}/promote", candidateId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reviewBody = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
        Assert.Contains("Promotion failed while creating the admin draft", reviewBody);
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

    private static async Task<int> SeedDiscoveredCandidateWithDraftAsync(InMemoryNewsDiscoveryRepository repository)
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "discovered-draft-source",
            "Discovered draft source",
            "https://example.com/",
            null,
            NewsDiscoverySourceType.AllowlistedPage,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var discoveredAt = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://example.com/discovered-with-draft",
            "Discovered with draft",
            discoveredAt,
            "Excerpt",
            discoveredAt));
        await repository.UpsertDraftAsync(
            candidateId,
            new NewsAgentDraftUpsert(
                "Undrafted promote title",
                "undrafted-promote-title",
                "Excerpt",
                "Body",
                null,
                null,
                null,
                discoveredAt.Date,
                null));
        return candidateId;
    }

    private WebApplicationFactory<Program> CreateFactory(
        SharedNewsStore newsStore,
        SharedNewsDiscoveryStore discoveryStore,
        string? openRouterApiKey = "test-key",
        string? draftResponseJson = null,
        bool aiClientEnabled = true,
        INewsDiscoveryRepository? discoveryRepository = null,
        IAdminNewsRepository? adminNewsRepository = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            if (openRouterApiKey is not null)
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["OpenRouter:ApiKey"] = openRouterApiKey
                    });
                });
            }

            var draftJson = draftResponseJson ?? NewsAgentTestSupport.SampleDraftJson;
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SharedNewsStore>();
                services.RemoveAll<SharedNewsDiscoveryStore>();
                services.RemoveAll<INewsRepository>();
                services.RemoveAll<IAdminNewsRepository>();
                services.RemoveAll<INewsAuditRepository>();
                services.RemoveAll<INewsDiscoveryRepository>();
                services.RemoveAll<INewsAiClient>();
                services.AddSingleton(newsStore);
                services.AddSingleton(discoveryStore);
                services.AddSingleton<INewsRepository>(_ => new QueenZone.Data.InMemoryNewsRepository(newsStore));
                services.AddSingleton<INewsAuditRepository>(_ => new InMemoryNewsAuditRepository(newsStore));
                if (adminNewsRepository is not null)
                {
                    services.AddSingleton(adminNewsRepository);
                }
                else
                {
                    services.AddSingleton<IAdminNewsRepository>(_ => new InMemoryAdminNewsRepository(newsStore));
                }

                if (discoveryRepository is not null)
                {
                    services.AddSingleton(discoveryRepository);
                }
                else
                {
                    services.AddSingleton<INewsDiscoveryRepository>(_ => new InMemoryNewsDiscoveryRepository(discoveryStore));
                }
                services.AddSingleton<INewsAiClient>(_ => new RegenerateDraftFakeAiClient(draftJson, aiClientEnabled));
            });
        });

    private HttpClient CreateClient(
        string? email = null,
        SharedNewsStore? newsStore = null,
        SharedNewsDiscoveryStore? discoveryStore = null,
        string? openRouterApiKey = "test-key",
        string? draftResponseJson = null,
        bool aiClientEnabled = true,
        INewsDiscoveryRepository? discoveryRepository = null,
        IAdminNewsRepository? adminNewsRepository = null)
    {
        newsStore ??= new SharedNewsStore();
        discoveryStore ??= new SharedNewsDiscoveryStore();
        var appFactory = CreateFactory(
            newsStore,
            discoveryStore,
            openRouterApiKey,
            draftResponseJson,
            aiClientEnabled,
            discoveryRepository,
            adminNewsRepository);
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

    private static async Task<HttpResponseMessage> PostAdminNewsActionAsync(HttpClient client, string actionPath)
    {
        var listPage = await client.GetStringAsync("/admin/news");
        var token = ExtractAntiforgeryToken(listPage);
        return await client.PostAsync(actionPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [QueenZone.Web.Pages.Admin.News.AdminNewsPageModel.AntiforgeryTokenFieldName] = token
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

    private sealed class RegenerateDraftFakeAiClient(string content, bool enabled = true) : INewsAiClient
    {
        public bool IsEnabled { get; } = enabled;

        public Task<NewsAiChatCompletion> CompleteChatAsync(
            NewsAiChatRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsAiChatCompletion(
                content,
                "openai/gpt-4.1-mini",
                1,
                1,
                0.0001m,
                false));
    }
}
