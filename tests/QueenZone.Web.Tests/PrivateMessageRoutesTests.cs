using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PrivateMessageRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PrivateMessageRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task Get_Messages_RedirectsUnauthenticatedUsersToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/messages");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Get_Inbox_ShowsEmptyState_ForSignedInMember()
    {
        var (client, _) = await CreateMemberAsync("empty-inbox@example.com", "Empty Inbox");

        var html = await client.GetStringAsync("/messages");

        Assert.Contains("You have no private messages yet", html);
        Assert.Contains("/messages/compose", html);
        Assert.Contains(">Messages<", html);
    }

    [Fact]
    public async Task Compose_Send_Reply_AndUnreadFlow_Works()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-alice@example.com", "PM Alice");
        var (bobClient, bob) = await CreateMemberAsync("pm-bob@example.com", "PM Bob");

        var composePage = await aliceClient.GetStringAsync($"/messages/compose?to={bob.Id}");
        Assert.Contains("PM Bob", composePage);

        var sendResponse = await aliceClient.PostAsync("/messages/compose", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(composePage),
            ["Input.RecipientMemberId"] = bob.Id.ToString(),
            ["Input.Body"] = "Hello from Alice",
        }));
        Assert.Equal(HttpStatusCode.Redirect, sendResponse.StatusCode);
        var conversationPath = sendResponse.Headers.Location!.OriginalString;
        Assert.StartsWith("/messages/", conversationPath);

        var bobInbox = await bobClient.GetStringAsync("/messages");
        Assert.Contains("PM Alice", bobInbox);
        Assert.Contains("Hello from Alice", bobInbox);
        Assert.Contains("unread", bobInbox, StringComparison.OrdinalIgnoreCase);

        var bobConversation = await bobClient.GetStringAsync(conversationPath);
        Assert.Contains("Hello from Alice", bobConversation);

        var messages = factory.Services.GetRequiredService<IPrivateMessageRepository>();
        Assert.Equal(0, await messages.CountUnreadConversationsAsync(bob.Id));

        var replyResponse = await bobClient.PostAsync(conversationPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(bobConversation),
            ["Input.Body"] = "Hello back",
        }));
        Assert.Equal(HttpStatusCode.Redirect, replyResponse.StatusCode);

        Assert.Equal(1, await messages.CountUnreadConversationsAsync(alice.Id));

        var aliceConversation = await aliceClient.GetStringAsync(conversationPath);
        Assert.Contains("Hello from Alice", aliceConversation);
        Assert.Contains("Hello back", aliceConversation);
        Assert.Equal(0, await messages.CountUnreadConversationsAsync(alice.Id));
    }

    [Fact]
    public async Task Get_Conversation_ReturnsNotFound_ForNonParticipant()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-owner@example.com", "Owner");
        var (bobClient, bob) = await CreateMemberAsync("pm-peer@example.com", "Peer");
        var (carolClient, _) = await CreateMemberAsync("pm-outsider@example.com", "Outsider");

        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Top secret");

        var response = await carolClient.GetAsync($"/messages/{created.ConversationId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MemberProfile_ShowsMessageAction_WhenAllowed()
    {
        var (aliceClient, alice) = await CreateMemberAsync("profile-alice@example.com", "Profile Alice");
        var (_, bob) = await CreateMemberAsync("profile-bob@example.com", "Profile Bob");

        var html = await aliceClient.GetStringAsync($"/members/{bob.Id}");
        Assert.Contains("Profile Bob", html);
        Assert.Contains($"/messages/compose?to={bob.Id}", html);
        Assert.Contains(">Message<", html);

        var selfHtml = await aliceClient.GetStringAsync($"/members/{alice.Id}");
        Assert.DoesNotContain($"/messages/compose?to={alice.Id}", selfHtml);
    }

    [Fact]
    public async Task MemberProfile_Unauthenticated_PromptsSignInToMessage()
    {
        var (_, bob) = await CreateMemberAsync("profile-public@example.com", "Public Bob");
        var anon = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var html = await anon.GetStringAsync($"/members/{bob.Id}");
        Assert.Contains("Sign in to message", html);
        Assert.Contains("/account/login", html);
    }

    private async Task<(HttpClient Client, MemberAccount Member)> CreateMemberAsync(
        string email,
        string displayName)
    {
        var memberId = Guid.NewGuid();
        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        var member = await members.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, member.Id.ToString());
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, member.DisplayName);
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.EmailHeader, member.Email);
        return (client, member);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            """(?:name=["']__RequestVerificationToken["'][^>]*value=["'](?<token>[^"']+)["'])|(?:value=["'](?<token>[^"']+)["'][^>]*name=["']__RequestVerificationToken["'])""",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Antiforgery token was not found in the form. Snip: " + SnipTokenArea(html));
        return match.Groups["token"].Value;
    }

    private static string SnipTokenArea(string html)
    {
        var idx = html.IndexOf("RequestVerification", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            idx = html.IndexOf("<form", StringComparison.OrdinalIgnoreCase);
        }

        if (idx < 0)
        {
            return html.Length <= 400 ? html : html[..400];
        }

        var start = Math.Max(0, idx - 80);
        return html.Substring(start, Math.Min(500, html.Length - start));
    }
}
