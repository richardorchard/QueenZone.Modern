using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class ForumTopicWatchApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public ForumTopicWatchApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Watch_RequiresMobileBearer_NotCookie()
    {
        using var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        using var cookieOnly = factory.CreateAnonymousClient(allowAutoRedirect: false);
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, Guid.NewGuid().ToString());
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Cookie Fan");

        foreach (var client in new[] { anonymous, cookieOnly })
        {
            using var get = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch");
            Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
            Assert.Equal("application/problem+json", get.Content.Headers.ContentType?.MediaType);

            using var post = await client.PostAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch", null);
            Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);

            using var delete = await client.DeleteAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch");
            Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
        }
    }

    [Fact]
    public async Task Member_CanWatchAndUnwatch_PublicTopic_Idempotently()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Watcher Fan");
        using var client = CreateBearerClient(memberId, "Watcher Fan");

        using var initial = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch");
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        Assert.Contains("no-store", initial.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var initialBody = await initial.Content.ReadFromJsonAsync<ForumTopicWatchDto>(JsonOptions);
        Assert.False(initialBody!.Watching);

        using var watched = await client.PostAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch", null);
        Assert.Equal(HttpStatusCode.OK, watched.StatusCode);
        Assert.True((await watched.Content.ReadFromJsonAsync<ForumTopicWatchDto>(JsonOptions))!.Watching);

        using var watchedAgain = await client.PostAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch", null);
        Assert.True((await watchedAgain.Content.ReadFromJsonAsync<ForumTopicWatchDto>(JsonOptions))!.Watching);

        using var afterWatch = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch");
        Assert.True((await afterWatch.Content.ReadFromJsonAsync<ForumTopicWatchDto>(JsonOptions))!.Watching);

        using var unwatched = await client.DeleteAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch");
        Assert.False((await unwatched.Content.ReadFromJsonAsync<ForumTopicWatchDto>(JsonOptions))!.Watching);

        using var unwatchedAgain = await client.DeleteAsync($"{ForumApiEndpoints.RootPath}/topics/1002/watch");
        Assert.False((await unwatchedAgain.Content.ReadFromJsonAsync<ForumTopicWatchDto>(JsonOptions))!.Watching);
    }

    [Fact]
    public async Task Watch_MissingTopic_ReturnsProblemDetailsNotFound()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Missing Topic Fan");
        using var client = CreateBearerClient(memberId, "Missing Topic Fan");

        using var response = await client.PostAsync($"{ForumApiEndpoints.RootPath}/topics/9999/watch", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
        Assert.Contains("9999", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApi_IncludesWatchGetPostDelete()
    {
        using var client = factory.CreateAnonymousClient();
        using var response = await client.GetAsync(ApiV1.OpenApiPath);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var path = payload.GetProperty("paths").GetProperty("/api/v1/forum/topics/{id}/watch");
        Assert.True(path.TryGetProperty("get", out _));
        Assert.True(path.TryGetProperty("post", out _));
        Assert.True(path.TryGetProperty("delete", out _));
    }

    private HttpClient CreateBearerClient(Guid memberId, string displayName)
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<MobileAuthTokenIssuer>();
        var token = issuer.IssueAccessToken(memberId, $"{memberId:N}@example.test", displayName);
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task SeedMemberAsync(Guid memberId, string displayName)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>();
        await repository.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = $"{memberId:N}@example.test",
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }
}
