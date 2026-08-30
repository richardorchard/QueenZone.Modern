using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class ForumApiWriteTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public ForumApiWriteTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_topic_and_reply_require_bearer_token()
    {
        using var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        using var cookieOnly = factory.CreateAnonymousClient(allowAutoRedirect: false);
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, Guid.NewGuid().ToString());
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Cookie Fan");

        foreach (var client in new[] { anonymous, cookieOnly })
        {
            using var topic = await client.PostAsJsonAsync(
                $"{ForumApiEndpoints.RootPath}/categories/1/topics",
                new { title = "Needs a token here", body = "Anonymous cannot write." });
            Assert.Equal(HttpStatusCode.Unauthorized, topic.StatusCode);
            Assert.Equal("application/problem+json", topic.Content.Headers.ContentType?.MediaType);

            using var reply = await client.PostAsJsonAsync(
                $"{ForumApiEndpoints.RootPath}/topics/1002/posts",
                new { body = "Anonymous cannot reply." });
            Assert.Equal(HttpStatusCode.Unauthorized, reply.StatusCode);
            Assert.Equal("application/problem+json", reply.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Member_can_create_topic_and_reply_with_same_storage_as_website()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "App Poster");
        using var client = CreateBearerClient(memberId, "App Poster");

        using var createdResponse = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics",
            new { title = "App started this thread", body = "Hello <strong>fans</strong><script>alert(1)</script>" });

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ForumTopicCreatedDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.True(created.StarterPostId > 0);
        Assert.Equal("App started this thread", created.Title);
        Assert.Equal(ForumRoutes.GetTopicCanonicalPath(created.Id, created.Title), created.DetailPath);
        Assert.Equal(
            $"{ForumApiEndpoints.RootPath}/topics/{created.Id}",
            createdResponse.Headers.Location?.OriginalString);

        using var replyResponse = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/topics/{created.Id}/posts",
            new { body = "Plain reply from the app" });
        Assert.Equal(HttpStatusCode.Created, replyResponse.StatusCode);
        var reply = await replyResponse.Content.ReadFromJsonAsync<ForumPostCreatedDto>(JsonOptions);
        Assert.NotNull(reply);
        Assert.Equal(created.Id, reply!.TopicId);
        Assert.Contains($"#post-{reply.Id}", reply.DetailPath, StringComparison.Ordinal);
        Assert.Equal(reply.DetailPath, replyResponse.Headers.Location?.OriginalString);

        using var postsResponse = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/{created.Id}/posts");
        var posts = await postsResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumPostDto>>(JsonOptions);
        Assert.NotNull(posts);
        Assert.Equal(2, posts!.TotalCount);
        Assert.Contains("Hello", posts.Items[0].Body, StringComparison.Ordinal);
        Assert.Contains("<strong>", posts.Items[0].Body, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", posts.Items[0].Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Plain reply from the app", posts.Items[1].Body, StringComparison.Ordinal);
        Assert.All(posts.Items, item => Assert.Equal("App Poster", item.AuthorUsername));

        var html = await factory.CreateAnonymousClient().GetStringAsync(created.DetailPath);
        Assert.Contains("App started this thread", html, StringComparison.Ordinal);
        Assert.Contains("Plain reply from the app", html, StringComparison.Ordinal);
        Assert.Contains("App Poster", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_topic_rejects_short_title_and_empty_sanitized_body()
    {
        using var client = CreateBearerClient(Guid.NewGuid());

        using var shortTitle = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics",
            new { title = "Hey", body = "This body is long enough." });
        Assert.Equal(HttpStatusCode.BadRequest, shortTitle.StatusCode);
        var shortProblem = await shortTitle.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            ForumPostWriteService.SubjectLengthMessage,
            shortProblem.GetProperty("detail").GetString(),
            StringComparison.Ordinal);

        using var emptyBody = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics",
            new { title = "A valid title here", body = "<script>alert(1)</script>" });
        Assert.Equal(HttpStatusCode.BadRequest, emptyBody.StatusCode);
        var emptyProblem = await emptyBody.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            ForumPostWriteService.BodyRequiredMessage,
            emptyProblem.GetProperty("detail").GetString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_topic_returns_not_found_for_missing_board()
    {
        using var client = CreateBearerClient(Guid.NewGuid());

        using var response = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/categories/9999/topics",
            new { title = "Missing board thread", body = "Should not persist." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Reply_returns_not_found_for_missing_topic()
    {
        using var client = CreateBearerClient(Guid.NewGuid());

        using var response = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/topics/9999/posts",
            new { body = "Missing topic reply." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("9999", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Topic_detail_includes_isLocked_when_thread_is_locked()
    {
        using var lockedFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<IForumWriteRepository>();
            services.AddSingleton<IForumWriteRepository>(new LockedForumWriteRepository());
        });
        using var client = lockedFactory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var topic = await response.Content.ReadFromJsonAsync<ForumTopicDetailDto>(JsonOptions);
        Assert.NotNull(topic);
        Assert.True(topic!.IsLocked);
    }

    [Fact]
    public async Task Reply_returns_forbidden_when_topic_is_locked()
    {
        using var lockedFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<IForumWriteRepository>();
            services.AddSingleton<IForumWriteRepository>(new LockedForumWriteRepository());
        });
        using var client = CreateBearerClient(lockedFactory, Guid.NewGuid());

        using var response = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/topics/1002/posts",
            new { body = "Should be blocked on a locked topic." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ForumPostWriteService.TopicLockedMessage,
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Reply_returns_forbidden_when_member_is_suspended()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Suspended Fan", isSuspended: true);
        using var client = CreateBearerClient(memberId, "Suspended Fan");

        using var response = await client.PostAsJsonAsync(
            $"{ForumApiEndpoints.RootPath}/topics/1002/posts",
            new { body = "Suspended members cannot post." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ForumPostWriteService.SuspendedMessage,
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Writes_return_too_many_requests_after_shared_rate_limit()
    {
        var memberId = Guid.NewGuid();
        using var client = CreateBearerClient(memberId);

        HttpResponseMessage? response = null;
        for (var i = 0; i < ForumPostRateLimiter.MaxPostsPerMinute + 1; i++)
        {
            response = await client.PostAsJsonAsync(
                $"{ForumApiEndpoints.RootPath}/topics/1002/posts",
                new { body = $"Rate-limit reply {i}" });
        }

        Assert.NotNull(response);
        Assert.Equal((HttpStatusCode)429, response!.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem.GetProperty("status").GetInt32());
        Assert.Equal(
            ForumPostWriteService.RateLimitedMessage,
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Multipart_write_reuses_attachment_validator()
    {
        using var client = CreateBearerClient(Guid.NewGuid());
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Attachment rejection thread", Encoding.UTF8), "title");
        content.Add(new StringContent("Body with a bad attachment", Encoding.UTF8), "body");
        var file = new ByteArrayContent("not-an-image"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
        content.Add(file, "attachments", "malware.exe");

        using var response = await client.PostAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("not allowed", problem.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateBearerClient(Guid memberId, string displayName = "Forum Fan") =>
        CreateBearerClient(factory, memberId, displayName);

    private HttpClient CreateBearerClient(
        QueenZoneWebApplicationFactory source,
        Guid memberId,
        string displayName = "Forum Fan")
    {
        using var scope = source.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<MobileAuthTokenIssuer>();
        var token = issuer.IssueAccessToken(memberId, $"{memberId:N}@example.test", displayName);
        var client = source.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task SeedMemberAsync(Guid memberId, string displayName, bool isSuspended = false)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>();
        await repository.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = $"{memberId:N}@example.test",
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            IsSuspended = isSuspended,
        });
    }

    private sealed class LockedForumWriteRepository : IForumWriteRepository
    {
        public Task<ForumThreadCreateResult> CreateThreadAsync(
            NewForumThread thread,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ForumThreadCreateResult(200_001, 2_000_001));

        public Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Locked.");

        public Task<ForumEditablePost?> GetPostAsync(int postId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ForumEditablePost?>(null);

        public Task<ForumPostUpdateResult> UpdatePostAsync(
            int postId,
            Guid editorMemberId,
            string sanitisedBody,
            bool isAdmin,
            int editWindowMinutes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.Forbidden));

        public Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ForumWriteThread?>(new ForumWriteThread(
                topicId,
                1,
                "Ranking every studio album",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                1,
                IsLocked: true));

        public Task<int> CountPostsByMemberSinceAsync(
            Guid memberId,
            DateTimeOffset since,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> CountApprovedPostsByMemberAsync(
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task HideAuthorForumContentAsync(Guid? memberId, string displayName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UnhideAuthorForumContentAsync(Guid? memberId, string displayName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> EnsureCategoryAsync(
            string slug,
            string name,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
