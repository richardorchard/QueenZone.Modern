using System.Net;
using System.Text.RegularExpressions;

namespace QueenZone.Web.Tests;

public sealed class ForumTopicWatchRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ForumTopicWatchRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task TopicPage_Anonymous_ShowsSignInToWatch()
    {
        using var client = factory.CreateAnonymousClient();
        var html = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");

        Assert.Contains("Sign in to watch", html, StringComparison.Ordinal);
        Assert.Contains("Watching a topic is how you opt in to reply pushes.", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Watch topic<", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Unwatch<", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TopicPage_Member_CanWatchAndUnwatch()
    {
        var memberId = Guid.NewGuid();
        var client = CreateMemberClient(memberId);
        var page = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        Assert.Contains(">Watch topic<", page, StringComparison.Ordinal);
        Assert.Contains("Posting here does not subscribe you.", page, StringComparison.Ordinal);
        var token = ExtractAntiforgeryToken(page);

        var watch = await client.PostAsync(
            "/forum/topic/1002/ranking-every-studio-album?handler=Watch",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));
        Assert.Equal(HttpStatusCode.Redirect, watch.StatusCode);
        Assert.Equal(
            "/forum/topic/1002/ranking-every-studio-album",
            watch.Headers.Location!.OriginalString);

        var watchingPage = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        Assert.Contains(">Unwatch<", watchingPage, StringComparison.Ordinal);
        Assert.Contains("You're watching this topic.", watchingPage, StringComparison.Ordinal);
        var unwatchToken = ExtractAntiforgeryToken(watchingPage);

        var unwatch = await client.PostAsync(
            "/forum/topic/1002/ranking-every-studio-album?handler=Unwatch",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = unwatchToken,
            }));
        Assert.Equal(HttpStatusCode.Redirect, unwatch.StatusCode);

        var after = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        Assert.Contains(">Watch topic<", after, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WatchPost_MissingTopic_ReturnsNotFound()
    {
        var client = CreateMemberClient(Guid.NewGuid());
        var page = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        var token = ExtractAntiforgeryToken(page);

        var response = await client.PostAsync(
            "/forum/topic/9999/missing-topic?handler=Watch",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WatchPost_Anonymous_ChallengesToLogin()
    {
        var client = CreateMemberClient(Guid.NewGuid());
        var page = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        var token = ExtractAntiforgeryToken(page);
        client.DefaultRequestHeaders.Remove(TestMemberAuthHandler.MemberIdHeader);
        client.DefaultRequestHeaders.Remove(TestMemberAuthHandler.DisplayNameHeader);

        var response = await client.PostAsync(
            "/forum/topic/1002/ranking-every-studio-album?handler=Watch",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TopicPageTwo_ShowsWatchControl()
    {
        using var client = factory.CreateAnonymousClient();
        var html = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album/page/2");
        Assert.Contains("Sign in to watch", html, StringComparison.Ordinal);
    }

    private HttpClient CreateMemberClient(Guid memberId, string displayName = "Forum Fan")
    {
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, memberId.ToString());
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, displayName);
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
}
