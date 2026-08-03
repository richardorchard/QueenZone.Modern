using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;
using QueenZone.Web.Pages.Members;

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
    public async Task Get_Inbox_PaginatesConversations()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-inbox-page-alice@example.com", "Inbox Alice");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();

        const int total = PrivateMessageLimits.InboxPageSize + 1;
        for (var i = 1; i <= total; i++)
        {
            var (_, peer) = await CreateMemberAsync($"pm-inbox-peer-{i}@example.com", $"Inbox Peer {i}");
            Assert.True((await service.ComposeAsync(alice.Id, peer.Id, $"Hello {i}")).Succeeded);
        }

        var page1 = await aliceClient.GetStringAsync("/messages");
        Assert.Contains("Inbox conversation pagination", page1);
        Assert.Contains("Page 1 of 2", page1);
        Assert.Contains("/messages?pageNumber=2", page1);
        Assert.Contains("Inbox Peer 51", page1);
        Assert.DoesNotContain("Inbox Peer 1</", page1);

        var page2 = await aliceClient.GetStringAsync("/messages?pageNumber=2");
        Assert.Contains("Page 2 of 2", page2);
        Assert.Contains("Inbox Peer 1</", page2);
    }

    [Fact]
    public async Task Get_Conversation_PaginatesMessages_AndDefaultsToLatestPage()
    {
        var (_, alice) = await CreateMemberAsync("pm-page-alice@example.com", "Page Alice");
        var (bobClient, bob) = await CreateMemberAsync("pm-page-bob@example.com", "Page Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();

        const int totalMessages = PrivateMessageLimits.ConversationPageSize + 1;
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Msg 1");
        var conversationId = created.ConversationId!.Value;
        for (var i = 2; i <= totalMessages; i++)
        {
            Assert.True((await service.ReplyAsync(conversationId, alice.Id, $"Msg {i}")).Succeeded);
        }

        var defaultHtml = await bobClient.GetStringAsync($"/messages/{conversationId}");
        Assert.Contains($"Msg {totalMessages}", defaultHtml);
        Assert.DoesNotContain(">Msg 1<", defaultHtml);
        Assert.Contains("Conversation message pagination", defaultHtml);
        Assert.Contains($"/messages/{conversationId}?pageNumber=1", defaultHtml);
        Assert.Contains("Page 2 of 2", defaultHtml);

        var pageOneHtml = await bobClient.GetStringAsync($"/messages/{conversationId}?pageNumber=1");
        Assert.Contains(">Msg 1<", pageOneHtml);
        Assert.DoesNotContain($">Msg {totalMessages}<", pageOneHtml);
        Assert.Contains("Page 1 of 2", pageOneHtml);
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
    public async Task Archive_FromInbox_HidesConversation_AndListsInArchivedView()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-archive-alice@example.com", "Archive Alice");
        var (bobClient, bob) = await CreateMemberAsync("pm-archive-bob@example.com", "Archive Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Archive this thread");
        var conversationId = created.ConversationId!.Value;

        var inboxHtml = await aliceClient.GetStringAsync("/messages");
        var archiveResponse = await aliceClient.PostAsync(
            "/messages?handler=Archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(inboxHtml),
                ["conversationId"] = conversationId.ToString(),
            }));
        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);

        var aliceInboxAfter = await aliceClient.GetStringAsync("/messages");
        Assert.Contains("You have no private messages yet", aliceInboxAfter);
        Assert.Contains("Conversation archived", aliceInboxAfter);

        var bobInbox = await bobClient.GetStringAsync("/messages");
        Assert.Contains("Archive Alice", bobInbox);

        var archivedPage = await aliceClient.GetStringAsync("/messages/archived");
        Assert.Contains("Archive Bob", archivedPage);

        var unarchiveResponse = await aliceClient.PostAsync(
            "/messages/archived?handler=Unarchive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(archivedPage),
                ["conversationId"] = conversationId.ToString(),
            }));
        Assert.Equal(HttpStatusCode.Redirect, unarchiveResponse.StatusCode);

        var aliceInboxRestored = await aliceClient.GetStringAsync("/messages");
        Assert.Contains("Archive Bob", aliceInboxRestored);
    }

    [Fact]
    public async Task Archive_ReceivingNewMessage_ReturnsConversationToInbox()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-reopen-alice@example.com", "Reopen Alice");
        var (bobClient, bob) = await CreateMemberAsync("pm-reopen-bob@example.com", "Reopen Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Hello");
        var conversationId = created.ConversationId!.Value;

        var inboxHtml = await aliceClient.GetStringAsync("/messages");
        await aliceClient.PostAsync(
            "/messages?handler=Archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(inboxHtml),
                ["conversationId"] = conversationId.ToString(),
            }));
        Assert.Contains("You have no private messages yet", await aliceClient.GetStringAsync("/messages"));

        Assert.True((await service.ReplyAsync(conversationId, bob.Id, "New reply")).Succeeded);

        var aliceInboxAfter = await aliceClient.GetStringAsync("/messages");
        Assert.Contains("Reopen Bob", aliceInboxAfter);
    }

    [Fact]
    public async Task Archive_FromConversationView_RedirectsToInbox()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-conv-archive-alice@example.com", "Conv Archive Alice");
        var (_, bob) = await CreateMemberAsync("pm-conv-archive-bob@example.com", "Conv Archive Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Hello there");
        var conversationId = created.ConversationId!.Value;

        var conversationHtml = await aliceClient.GetStringAsync($"/messages/{conversationId}");
        var archiveResponse = await aliceClient.PostAsync(
            $"/messages/{conversationId}?handler=Archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(conversationHtml),
            }));
        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);
        Assert.Equal("/messages", archiveResponse.Headers.Location!.OriginalString);

        Assert.Contains("You have no private messages yet", await aliceClient.GetStringAsync("/messages"));
    }

    [Fact]
    public async Task Archive_ReturnsNotFound_ForNonParticipant()
    {
        var (aliceClient, alice) = await CreateMemberAsync("pm-archive-owner@example.com", "Archive Owner");
        var (_, bob) = await CreateMemberAsync("pm-archive-peer@example.com", "Archive Peer");
        var (carolClient, _) = await CreateMemberAsync("pm-archive-outsider@example.com", "Archive Outsider");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Top secret");

        var composeHtml = await carolClient.GetStringAsync("/messages/compose");
        var response = await carolClient.PostAsync(
            "/messages?handler=Archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(composeHtml),
                ["conversationId"] = created.ConversationId.ToString()!,
            }));
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

    [Fact]
    public async Task MemberProfile_ListsAndPaginatesPublicActivity()
    {
        var memberId = Guid.NewGuid();
        var activity = Enumerable.Range(1, ProfileModel.ActivityPageSize + 1)
            .Select(index => new MemberPublicActivityItem(
                MemberPublicActivityType.ForumPost,
                $"Topic {index}",
                $"Post summary {index}",
                DateTimeOffset.UtcNow.AddMinutes(-index),
                ContentId: index,
                ParentId: 1000 + index,
                Slug: $"topic-{index}"))
            .ToList();
        using var profileFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMemberPublicActivityRepository>();
            services.AddSingleton<IMemberPublicActivityRepository>(new StubMemberPublicActivityRepository(activity));
        }));
        var members = profileFactory.Services.GetRequiredService<IMemberAccountRepository>();
        await members.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = "activity@example.com",
            DisplayName = "Active Member",
            CreatedAt = DateTime.UtcNow,
        });
        var client = profileFactory.CreateClient();

        var firstPage = await client.GetStringAsync($"/members/{memberId}");
        Assert.Contains("Public contributions", firstPage);
        Assert.Contains("Topic 1", firstPage);
        Assert.DoesNotContain("Topic 21", firstPage);
        Assert.Contains("Member activity pagination", firstPage);

        var secondPage = await client.GetStringAsync($"/members/{memberId}?pageNumber=2");
        Assert.Contains("Topic 21", secondPage);
        Assert.DoesNotContain("Topic 1</a>", secondPage);
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

    private sealed class StubMemberPublicActivityRepository(IReadOnlyList<MemberPublicActivityItem> items)
        : IMemberPublicActivityRepository
    {
        public Task<MemberPublicActivityPage> GetPageAsync(
            Guid memberId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new MemberPublicActivityPage(pageItems, items.Count, page, pageSize));
        }
    }
}
