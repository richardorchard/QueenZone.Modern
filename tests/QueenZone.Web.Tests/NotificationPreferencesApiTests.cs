using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NotificationPreferencesApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public NotificationPreferencesApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Get_RequiresMobileBearer_NotCookie()
    {
        using var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        using var cookieOnly = factory.CreateAnonymousClient(allowAutoRedirect: false);
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, Guid.NewGuid().ToString());
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Cookie Fan");

        foreach (var client in new[] { anonymous, cookieOnly })
        {
            using var response = await client.GetAsync(NotificationPreferencesApiEndpoints.Path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Patch_RequiresMobileBearer()
    {
        using var client = factory.CreateAnonymousClient(allowAutoRedirect: false);

        using var response = await client.PatchAsJsonAsync(
            NotificationPreferencesApiEndpoints.Path,
            new { news = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_MissingMemberAccount_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(Guid.NewGuid(), "Ghost Fan", "ghost-prefs@example.com");

        using var response = await client.GetAsync(NotificationPreferencesApiEndpoints.Path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ReturnsDefaults_WithNoStore()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Default Fan", "prefs-default@example.com");
        using var client = CreateBearerClient(memberId, "Default Fan", "prefs-default@example.com");

        using var response = await client.GetAsync(NotificationPreferencesApiEndpoints.Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.True(body!.ForumReply);
        Assert.True(body.PrivateMessage);
        Assert.False(body.News);
    }

    [Fact]
    public async Task Patch_Partial_UpdatesOnlySuppliedFields()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Partial Fan", "prefs-partial@example.com");
        using var client = CreateBearerClient(memberId, "Partial Fan", "prefs-partial@example.com");

        using var patched = await client.PatchAsJsonAsync(
            NotificationPreferencesApiEndpoints.Path,
            new { news = true });

        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        var afterNews = await patched.Content.ReadFromJsonAsync<NotificationPreferencesResponse>(JsonOptions);
        Assert.True(afterNews!.ForumReply);
        Assert.True(afterNews.PrivateMessage);
        Assert.True(afterNews.News);

        using var second = await client.PatchAsJsonAsync(
            NotificationPreferencesApiEndpoints.Path,
            new { forumReply = false });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var afterMute = await second.Content.ReadFromJsonAsync<NotificationPreferencesResponse>(JsonOptions);
        Assert.False(afterMute!.ForumReply);
        Assert.True(afterMute.PrivateMessage);
        Assert.True(afterMute.News);
    }

    [Fact]
    public async Task Patch_EmptyBody_ReturnsBadRequest()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Empty Prefs", "prefs-empty@example.com");
        using var client = CreateBearerClient(memberId, "Empty Prefs", "prefs-empty@example.com");

        using var emptyObject = await client.PatchAsJsonAsync(NotificationPreferencesApiEndpoints.Path, new { });
        Assert.Equal(HttpStatusCode.BadRequest, emptyObject.StatusCode);
        Assert.Equal("application/problem+json", emptyObject.Content.Headers.ContentType?.MediaType);
        var emptyProblem = await emptyObject.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("notification preference", emptyProblem.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);

        using var noBody = await client.PatchAsync(
            NotificationPreferencesApiEndpoints.Path,
            new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, noBody.StatusCode);
    }

    [Fact]
    public async Task Patch_ToDefault_PersistsARow()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Default Choice", "prefs-row@example.com");
        using var client = CreateBearerClient(memberId, "Default Choice", "prefs-row@example.com");

        using var response = await client.PatchAsJsonAsync(
            NotificationPreferencesApiEndpoints.Path,
            new { forumReply = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>(JsonOptions);
        Assert.True(body!.ForumReply);

        var rows = FindRows(memberId);
        var forum = Assert.Single(rows, row => row.Category == NotificationCategory.ForumReply);
        Assert.True(forum.IsEnabled);
    }

    [Fact]
    public async Task Members_CannotReadOrWriteEachOthersPreferences()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await SeedMemberAsync(firstId, "First Prefs", "prefs-a@example.com");
        await SeedMemberAsync(secondId, "Second Prefs", "prefs-b@example.com");

        using (var firstClient = CreateBearerClient(firstId, "First Prefs", "prefs-a@example.com"))
        {
            using var patched = await firstClient.PatchAsJsonAsync(
                NotificationPreferencesApiEndpoints.Path,
                new { news = true, forumReply = false });
            Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        }

        using var secondClient = CreateBearerClient(secondId, "Second Prefs", "prefs-b@example.com");
        using var response = await secondClient.GetAsync(NotificationPreferencesApiEndpoints.Path);
        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>(JsonOptions);
        Assert.True(body!.ForumReply);
        Assert.True(body.PrivateMessage);
        Assert.False(body.News);
        Assert.Empty(FindRows(secondId));
        Assert.NotEmpty(FindRows(firstId));
    }

    [Fact]
    public async Task OpenApi_IncludesGetAndPatch()
    {
        using var client = factory.CreateAnonymousClient();
        using var response = await client.GetAsync(ApiV1.OpenApiPath);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var path = payload.GetProperty("paths").GetProperty("/api/v1/me/notification-preferences");
        Assert.True(path.TryGetProperty("get", out _));
        Assert.True(path.TryGetProperty("patch", out _));
    }

    private HttpClient CreateBearerClient(Guid memberId, string displayName, string email)
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<MobileAuthTokenIssuer>();
        var token = issuer.IssueAccessToken(memberId, email, displayName);
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task SeedMemberAsync(Guid memberId, string displayName, string email)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>();
        await repository.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private List<NotificationPreferenceEntity> FindRows(Guid memberId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SharedNotificationPreferenceStore>();
        lock (store.Gate)
        {
            return store.Rows.Where(row => row.MemberAccountId == memberId).ToList();
        }
    }
}
