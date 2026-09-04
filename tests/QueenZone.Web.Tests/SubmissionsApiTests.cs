using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class SubmissionsApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public SubmissionsApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Anonymous_and_invalid_bearer_return_problem_details()
    {
        using var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        using var anonymousResponse = await anonymous.GetAsync(SubmissionsApiEndpoints.PhotosPath);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal("application/problem+json", anonymousResponse.Content.Headers.ContentType?.MediaType);

        using var invalid = factory.CreateAnonymousClient(allowAutoRedirect: false);
        invalid.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
        using var invalidResponse = await invalid.GetAsync(SubmissionsApiEndpoints.PhotosPath);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
        Assert.Equal("application/problem+json", invalidResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Photos_return_only_the_signed_in_members_rows_and_current_status()
    {
        var owner = await CreateMemberAsync("subs-photos-owner@example.com", "Owner Fan");
        var other = await CreateMemberAsync("subs-photos-other@example.com", "Other Fan");
        var photos = factory.Services.GetRequiredService<IPhotoSubmissionRepository>();

        var mine = await photos.CreateAsync(NewPhoto(owner.Id, "Owner exclusive photo"));
        await photos.CreateAsync(NewPhoto(other.Id, "Other member photo secret"));
        var rejected = await photos.UpdateStatusAsync(
            mine.Id,
            PhotoSubmissionStatus.Rejected,
            "admin@test.local",
            null,
            "Too dark");

        using var client = CreateMemberClient(owner);
        using var response = await client.GetAsync($"{SubmissionsApiEndpoints.PhotosPath}?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<PhotoSubmissionItemDto>>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload!.Items, row => row.Title == "Owner exclusive photo");
        Assert.DoesNotContain(payload.Items, row => row.Title == "Other member photo secret");
        Assert.Equal(PhotoSubmissionStatus.Rejected, item.Status.Status);
        Assert.Equal("Rejected", item.Status.StatusLabel);
        Assert.Equal("danger", item.Status.StatusTone);
        Assert.Equal("Too dark", item.Notes);
        Assert.Equal(
            UgcProxyPaths.GetPath(BlobUploadContainers.Photos, rejected!.ThumbnailBlobPath),
            item.ThumbnailPath);
    }

    [Fact]
    public async Task Photos_status_change_is_visible_on_the_next_request()
    {
        var member = await CreateMemberAsync("subs-photos-refresh@example.com", "Refresh Fan");
        var photos = factory.Services.GetRequiredService<IPhotoSubmissionRepository>();
        var created = await photos.CreateAsync(NewPhoto(member.Id, "Pending then approved"));

        using var client = CreateMemberClient(member);
        using var pendingResponse = await client.GetAsync(SubmissionsApiEndpoints.PhotosPath);
        var pending = await pendingResponse.Content.ReadFromJsonAsync<ApiPagedResponse<PhotoSubmissionItemDto>>();
        var pendingItem = Assert.Single(pending!.Items, row => row.Id == created.Id);
        Assert.Equal(PhotoSubmissionStatus.Pending, pendingItem.Status.Status);

        await photos.UpdateStatusAsync(
            created.Id,
            PhotoSubmissionStatus.Approved,
            "admin@test.local",
            "Looks good",
            null,
            "Queen");

        using var approvedResponse = await client.GetAsync(SubmissionsApiEndpoints.PhotosPath);
        var approved = await approvedResponse.Content.ReadFromJsonAsync<ApiPagedResponse<PhotoSubmissionItemDto>>();
        var approvedItem = Assert.Single(approved!.Items, row => row.Id == created.Id);
        Assert.Equal(PhotoSubmissionStatus.Approved, approvedItem.Status.Status);
        Assert.Equal("Approved", approvedItem.Status.StatusLabel);
        Assert.Equal("success", approvedItem.Status.StatusTone);
        Assert.Equal("Looks good", approvedItem.Notes);
    }

    [Fact]
    public async Task FanPerformances_return_rejection_notes_and_published_path()
    {
        var owner = await CreateMemberAsync("subs-fp-owner@example.com", "Owner Fan");
        var other = await CreateMemberAsync("subs-fp-other@example.com", "Other Fan");
        var performances = factory.Services.GetRequiredService<IFanPerformanceSubmissionRepository>();

        var mine = await performances.CreateAsync(NewFanPerformance(owner.Id, "Owner cover"));
        await performances.CreateAsync(NewFanPerformance(other.Id, "Other cover secret"));
        await performances.PromoteAsync(mine.Id, 187, "admin@test.local", "Published");

        var rejected = await performances.CreateAsync(NewFanPerformance(owner.Id, "Rejected cover"));
        await performances.UpdateStatusAsync(
            rejected.Id,
            FanPerformanceSubmissionStatus.Rejected,
            "admin@test.local",
            null,
            "Too quiet");

        using var client = CreateMemberClient(owner);
        using var response = await client.GetAsync($"{SubmissionsApiEndpoints.FanPerformancesPath}?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<FanPerformanceSubmissionItemDto>>();
        Assert.NotNull(payload);
        Assert.DoesNotContain(payload!.Items, row => row.Title == "Other cover secret");
        var approved = Assert.Single(payload.Items, row => row.Title == "Owner cover");
        Assert.Equal(187, approved.PromotedStageId);
        Assert.Equal("/fan-performances#fan-performance-187", approved.PublishedPath);
        var denied = Assert.Single(payload.Items, row => row.Title == "Rejected cover");
        Assert.Equal("Too quiet", denied.Notes);
        Assert.Equal("Too quiet", denied.RejectionReason);
    }

    [Fact]
    public async Task News_and_articles_match_my_submissions_fields_including_published_links()
    {
        var owner = await CreateMemberAsync("subs-mixed-owner@example.com", "Owner Fan");
        var other = await CreateMemberAsync("subs-mixed-other@example.com", "Other Fan");
        var suggestions = factory.Services.GetRequiredService<INewsSuggestionRepository>();
        var articles = factory.Services.GetRequiredService<IArticleSubmissionRepository>();

        var mineNews = await suggestions.CreateAsync(NewSuggestion(
            owner.Id,
            "https://example.com/owner-exclusive-news-story",
            "Owner news"));
        await suggestions.CreateAsync(NewSuggestion(
            other.Id,
            "https://example.com/other-member-news-secret",
            "Other news"));
        await suggestions.PromoteAsync(mineNews.Id, 1003, "admin@test.local", "Promoted");

        var mineArticle = await articles.UpsertDraftAsync(new ArticleSubmissionDraft(
            null,
            owner.Id,
            "Owner exclusive article",
            "Excerpt",
            new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars),
            null,
            null));
        await articles.UpsertDraftAsync(new ArticleSubmissionDraft(
            null,
            other.Id,
            "Other member article secret",
            "Excerpt",
            new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars),
            null,
            null));
        await articles.UpdateStatusAsync(
            mineArticle.Id,
            ArticleSubmissionStatus.Published,
            "admin@test.local",
            "Ready",
            null);

        using var client = CreateMemberClient(owner);

        using var newsResponse = await client.GetAsync(SubmissionsApiEndpoints.NewsPath);
        var news = await newsResponse.Content.ReadFromJsonAsync<ApiPagedResponse<NewsSuggestionItemDto>>();
        var newsItem = Assert.Single(news!.Items, row => row.Title == "Owner news");
        Assert.DoesNotContain(news.Items, row => row.Url.Contains("other-member-news-secret", StringComparison.Ordinal));
        Assert.Equal(NewsSuggestionStatus.Promoted, newsItem.Status.Status);
        Assert.Equal("Promoted", newsItem.Status.StatusLabel);
        Assert.Equal(1003, newsItem.PublishedNewsId);
        Assert.StartsWith("/news/1003/", newsItem.PublishedPath);
        Assert.Equal("Promoted", newsItem.Notes);

        using var articlesResponse = await client.GetAsync(SubmissionsApiEndpoints.ArticlesPath);
        var articlePage = await articlesResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ArticleSubmissionItemDto>>();
        var articleItem = Assert.Single(articlePage!.Items, row => row.Title == "Owner exclusive article");
        Assert.DoesNotContain(articlePage.Items, row => row.Title == "Other member article secret");
        Assert.Equal(ArticleSubmissionStatus.Published, articleItem.Status.Status);
        Assert.False(articleItem.CanContinueEditing);
        Assert.Equal(
            ArticlesRoutes.GetCommunityArticleDetailPath(mineArticle.Slug),
            articleItem.PublishedPath);
    }

    [Fact]
    public async Task News_payload_json_matches_the_pre_batch_lookup_contract()
    {
        var submittedAt = new DateTimeOffset(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);
        var owner = await CreateMemberAsync("subs-news-json@example.com", "Json Fan");
        var suggestions = factory.Services.GetRequiredService<INewsSuggestionRepository>();
        var publishedId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var missingId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        var pendingId = Guid.Parse("cccccccc-dddd-eeee-ffff-aaaaaaaaaaaa");

        await suggestions.CreateAsync(NewSuggestion(
            owner.Id,
            "https://example.com/json-published-news",
            "Published news",
            publishedId,
            submittedAt));
        await suggestions.PromoteAsync(publishedId, 1003, "admin@test.local", "Promoted");

        await suggestions.CreateAsync(NewSuggestion(
            owner.Id,
            "https://example.com/json-missing-news",
            "Missing news",
            missingId,
            submittedAt.AddMinutes(-1)));
        await suggestions.PromoteAsync(missingId, 999_999, "admin@test.local", "Gone");

        await suggestions.CreateAsync(NewSuggestion(
            owner.Id,
            "https://example.com/json-pending-news",
            "Pending news",
            pendingId,
            submittedAt.AddMinutes(-2)));

        using var client = CreateMemberClient(owner);
        using var response = await client.GetAsync(SubmissionsApiEndpoints.NewsPath);
        var actualJson = await response.Content.ReadAsStringAsync();

        var publishedArticle = await factory.Services.GetRequiredService<INewsRepository>().GetByIdAsync(1003);
        Assert.NotNull(publishedArticle);
        var publishedPath = NewsRoutes.GetNewsDetailPath(publishedArticle!);
        var expected = ApiPagedResponse<NewsSuggestionItemDto>.Create(
            [
                new NewsSuggestionItemDto(
                    publishedId,
                    "https://example.com/json-published-news",
                    "https://example.com/json-published-news",
                    "Published news",
                    submittedAt,
                    new SubmissionStatusDto(NewsSuggestionStatus.Promoted, "Promoted", "success"),
                    "Promoted",
                    1003,
                    publishedPath),
                new NewsSuggestionItemDto(
                    missingId,
                    "https://example.com/json-missing-news",
                    "https://example.com/json-missing-news",
                    "Missing news",
                    submittedAt.AddMinutes(-1),
                    new SubmissionStatusDto(NewsSuggestionStatus.Promoted, "Promoted", "success"),
                    "Gone",
                    999_999,
                    null),
                new NewsSuggestionItemDto(
                    pendingId,
                    "https://example.com/json-pending-news",
                    "https://example.com/json-pending-news",
                    "Pending news",
                    submittedAt.AddMinutes(-2),
                    new SubmissionStatusDto(NewsSuggestionStatus.Pending, "Pending", "pending"),
                    null,
                    null,
                    null),
            ],
            page: 1,
            pageSize: ApiPagination.DefaultPageSize,
            totalCount: 3);
        var expectedJson = JsonSerializer.Serialize(expected, JsonApiOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedJson, actualJson);
    }

    [Fact]
    public async Task GetNewsAsync_loads_promoted_articles_with_one_batch_lookup()
    {
        var memberId = Guid.NewGuid();
        var suggestions = new InMemoryNewsSuggestionRepository();
        var news = new CountingBatchNewsRepository();
        for (var i = 0; i < 8; i++)
        {
            var created = await suggestions.CreateAsync(NewSuggestion(
                memberId,
                $"https://example.com/batch-news-{i}",
                $"Batch {i}"));
            await suggestions.PromoteAsync(created.Id, 4100 + i, "admin@test.local", null);
            news.Add(new NewsItem(
                4100 + i,
                $"Headline {i}",
                "excerpt",
                "body",
                DateTime.UtcNow,
                null,
                true,
                $"headline-{i}"));
        }

        var pending = await suggestions.CreateAsync(NewSuggestion(
            memberId,
            "https://example.com/batch-news-pending",
            "Pending"));
        var duplicatePromoted = await suggestions.CreateAsync(NewSuggestion(
            memberId,
            "https://example.com/batch-news-dup",
            "Duplicate target"));
        await suggestions.PromoteAsync(duplicatePromoted.Id, 4100, "admin@test.local", null);

        var context = MemberHttpContext(memberId);
        var result = await SubmissionsApiEndpoints.GetNewsAsync(
            context,
            suggestions,
            news,
            page: 1,
            pageSize: 20,
            CancellationToken.None);

        var payload = Assert.IsType<Ok<ApiPagedResponse<NewsSuggestionItemDto>>>(result).Value;
        Assert.Equal(1, news.GetByIdsCallCount);
        Assert.Equal(0, news.GetByIdCallCount);
        Assert.Equal(8, news.LastRequestedIds.Count);
        Assert.Equal(10, payload!.TotalCount);
        Assert.Equal(9, payload.Items.Count(item => item.PublishedPath is not null));
        Assert.Contains(payload.Items, item => item.Id == pending.Id && item.PublishedPath is null);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetNewsAsync_skips_the_news_lookup_when_the_page_has_no_promoted_ids()
    {
        var memberId = Guid.NewGuid();
        var suggestions = new InMemoryNewsSuggestionRepository();
        await suggestions.CreateAsync(NewSuggestion(
            memberId,
            "https://example.com/pending-only",
            "Pending only"));
        var news = new CountingBatchNewsRepository();
        var context = MemberHttpContext(memberId);

        var result = await SubmissionsApiEndpoints.GetNewsAsync(
            context,
            suggestions,
            news,
            page: 1,
            pageSize: 20,
            CancellationToken.None);

        Assert.IsType<Ok<ApiPagedResponse<NewsSuggestionItemDto>>>(result);
        Assert.Equal(0, news.GetByIdsCallCount);
        Assert.Equal(0, news.GetByIdCallCount);
    }

    [Fact]
    public async Task List_clamps_invalid_paging_query_values()
    {
        var member = await CreateMemberAsync("subs-paging@example.com", "Paging Fan");
        using var client = CreateMemberClient(member);

        using var response = await client.GetAsync($"{SubmissionsApiEndpoints.PhotosPath}?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<PhotoSubmissionItemDto>>();
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
    }

    [Fact]
    public async Task OpenApi_includes_member_submission_routes()
    {
        using var client = factory.CreateAnonymousClient();
        using var response = await client.GetAsync(ApiV1.OpenApiPath);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = payload.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/me/submissions/photos", out _));
        Assert.True(paths.TryGetProperty("/api/v1/me/submissions/news", out _));
        Assert.True(paths.TryGetProperty("/api/v1/me/submissions/articles", out _));
    }

    [Fact]
    public void UnauthorizedIfMissingMember_returns_problem_when_name_identifier_is_not_a_guid()
    {
        var failure = SubmissionsApiEndpoints.UnauthorizedIfMissingMember(
            new DefaultHttpContext(),
            out var memberId);

        Assert.Equal(Guid.Empty, memberId);
        Assert.NotNull(failure);
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(failure);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void UnauthorizedIfMissingMember_accepts_a_guid_name_identifier()
    {
        var id = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, id.ToString("D"))],
                "test")),
        };

        var failure = SubmissionsApiEndpoints.UnauthorizedIfMissingMember(context, out var memberId);

        Assert.Null(failure);
        Assert.Equal(id, memberId);
    }

    private static readonly JsonSerializerOptions JsonApiOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static DefaultHttpContext MemberHttpContext(Guid memberId) =>
        new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, memberId.ToString("D"))],
                "test")),
        };

    private HttpClient CreateMemberClient(MemberAccount member)
    {
        var token = factory.Services.GetRequiredService<MobileAuthTokenIssuer>()
            .IssueAccessToken(member.Id, member.Email, member.DisplayName);
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<MemberAccount> CreateMemberAsync(string email, string displayName)
    {
        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        return await members.CreateAsync(new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private static NewFanPerformanceSubmission NewFanPerformance(Guid memberId, string title) =>
        new(
            memberId,
            title,
            "Reaching Out",
            "Owner Fan",
            null,
            $"members/{memberId:N}/cover.mp3",
            "cover.mp3",
            2048,
            "audio/mpeg",
            120,
            DateTimeOffset.UtcNow,
            FanPerformanceSubmissionRights.DeclarationVersion);

    private static NewPhotoSubmission NewPhoto(Guid memberId, string title) =>
        new(
            memberId,
            title,
            null,
            "Queen",
            1986,
            null,
            $"members/{memberId:N}/original.jpg",
            $"members/{memberId:N}/display.webp",
            $"members/{memberId:N}/thumb.webp",
            "shot.jpg",
            1024,
            "image/jpeg",
            800,
            600);

    private static NewsSuggestion NewSuggestion(
        Guid memberId,
        string url,
        string title,
        Guid? id = null,
        DateTimeOffset? submittedAt = null) =>
        new(
            id ?? Guid.NewGuid(),
            memberId,
            url,
            NewsCandidateDedupe.ComputeUrlHash(NewsCandidateDedupe.NormalizeCanonicalUrl(url)),
            title,
            null,
            NewsSuggestionStatus.Pending,
            submittedAt ?? DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            "Fan",
            null);

    private sealed class CountingBatchNewsRepository : INewsRepository
    {
        private readonly Dictionary<int, NewsItem> items = [];

        public int GetByIdCallCount { get; private set; }

        public int GetByIdsCallCount { get; private set; }

        public IReadOnlyList<int> LastRequestedIds { get; private set; } = [];

        public void Add(NewsItem item) => items[item.Id] = item;

        public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewsItem>>([]);

        public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(
            int page,
            int pageSize,
            NewsArchiveFilter filter = default,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewsItem>>([]);

        public Task<int> GetPublishedCountAsync(NewsArchiveFilter filter = default, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<NewsArchiveYearRange> GetArchiveYearRangeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsArchiveYearRange(null, null));

        public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(items.GetValueOrDefault(id));
        }

        public Task<IReadOnlyList<NewsItem>> GetByIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default)
        {
            GetByIdsCallCount++;
            LastRequestedIds = ids.ToArray();
            return Task.FromResult<IReadOnlyList<NewsItem>>(
                ids.Distinct().Where(items.ContainsKey).Select(id => items[id]).ToList());
        }

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>([]);

        public Task<NewsSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsSearchPage([], 0, page, pageSize));
    }
}
