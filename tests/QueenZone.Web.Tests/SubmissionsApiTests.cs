using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

    private static NewsSuggestion NewSuggestion(Guid memberId, string url, string title) =>
        new(
            Guid.NewGuid(),
            memberId,
            url,
            NewsCandidateDedupe.ComputeUrlHash(NewsCandidateDedupe.NormalizeCanonicalUrl(url)),
            title,
            null,
            NewsSuggestionStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            "Fan",
            null);
}
