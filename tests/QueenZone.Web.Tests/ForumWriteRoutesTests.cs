using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumWriteRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ForumWriteRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task NewThreadGet_RedirectsAnonymousMemberToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/forum/c/the-music/new-thread");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ValidNewThreadPost_CreatesThreadAndRedirectsToExistingTopicRoute()
    {
        var client = CreateMemberClient(factory, Guid.NewGuid());
        var form = await client.GetStringAsync("/forum/c/the-music/new-thread");
        var token = ExtractAntiforgeryToken(form);

        var response = await client.PostAsync("/forum/c/the-music/new-thread", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Subject"] = "Fresh forum news",
            ["Body"] = "<p>Hello <strong>fans</strong><script>alert(1)</script></p>",
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/forum/topic/", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExistingTopicPost_CreatesReplyAndRedirectsToPostAnchor()
    {
        var client = CreateMemberClient(factory, Guid.NewGuid());
        var page = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        var token = ExtractAntiforgeryToken(page);

        var response = await client.PostAsync("/forum/topic/1002/ranking-every-studio-album", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Body"] = "<p>New reply<script>alert(1)</script></p>",
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/forum/topic/1002/ranking-every-studio-album", response.Headers.Location!.OriginalString);
        Assert.Contains("#post-", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task NewThreadButton_IsVisibleOnCategoryPageForAuthenticatedMember()
    {
        var client = CreateMemberClient(factory, Guid.NewGuid());

        var body = await client.GetStringAsync("/forum/1/the-music");

        Assert.Contains("href=\"/forum/c/the-music/new-thread\"", body);
        Assert.Contains(">New thread<", body);
    }

    [Fact]
    public async Task LockedTopicPost_ReturnsForbidden()
    {
        var lockedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IForumWriteRepository>();
                services.AddSingleton<IForumWriteRepository>(new LockedForumWriteRepository());
            });
        });
        var client = CreateMemberClient(lockedFactory, Guid.NewGuid());
        var page = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        var token = ExtractAntiforgeryToken(page);

        var response = await client.PostAsync("/forum/topic/1002/ranking-every-studio-album", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Body"] = "<p>Nope</p>",
        }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient CreateMemberClient(WebApplicationFactory<Program> sourceFactory, Guid memberId)
    {
        var client = sourceFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, memberId.ToString());
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Forum Fan");
        return client;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var input = Regex.Match(
            html,
            """<input[^>]*name="__RequestVerificationToken"[^>]*>""",
            RegexOptions.IgnoreCase);
        Assert.True(input.Success, "Antiforgery token input was not found in the form.");

        var value = Regex.Match(input.Value, "value=\"(?<token>[^\"]+)\"", RegexOptions.IgnoreCase);
        Assert.True(value.Success, "Antiforgery token value was not found in the form.");
        return value.Groups["token"].Value;
    }

    private sealed class LockedForumWriteRepository : IForumWriteRepository
    {
        public Task<int> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default) =>
            Task.FromResult(200_001);

        public Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Locked.");

        public Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ForumWriteThread?>(new ForumWriteThread(
                topicId,
                1,
                "Ranking every studio album",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                1,
                IsLocked: true));

        public Task<int> CountPostsByMemberSinceAsync(Guid memberId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
