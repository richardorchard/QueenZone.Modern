using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class MessagesApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public MessagesApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Inbox_RequiresMobileBearer_NotCookie()
    {
        using var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        using var cookieOnly = factory.CreateAnonymousClient(allowAutoRedirect: false);
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, Guid.NewGuid().ToString());
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Cookie Fan");

        foreach (var client in new[] { anonymous, cookieOnly })
        {
            using var inbox = await client.GetAsync(MessagesApiEndpoints.Path);
            Assert.Equal(HttpStatusCode.Unauthorized, inbox.StatusCode);
            Assert.Equal("application/problem+json", inbox.Content.Headers.ContentType?.MediaType);

            using var unread = await client.GetAsync(MessagesApiEndpoints.UnreadCountPath);
            Assert.Equal(HttpStatusCode.Unauthorized, unread.StatusCode);
        }
    }

    [Fact]
    public async Task Inbox_Empty_UsesWebsitePageSize()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Empty Inbox", "api-empty-inbox@example.com");
        using var client = CreateBearerClient(memberId, "Empty Inbox", "api-empty-inbox@example.com");

        using var response = await client.GetAsync(MessagesApiEndpoints.Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var page = await response.Content.ReadFromJsonAsync<ApiPagedResponse<InboxConversationDto>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Empty(page!.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(PrivateMessageLimits.InboxPageSize, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task Inbox_UnreadCounts_MatchWebsiteAndHeaderBadge()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-unread-alice@example.com",
            "API Unread Alice",
            "api-unread-bob@example.com",
            "API Unread Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Hello from Alice");
        Assert.True(sent.Succeeded);

        using var bobApi = CreateBearerClient(bobId, "API Unread Bob", "api-unread-bob@example.com");
        using var bobWeb = CreateCookieClient(bobId, "API Unread Bob", "api-unread-bob@example.com");

        using var inboxResponse = await bobApi.GetAsync(MessagesApiEndpoints.Path);
        var inbox = await inboxResponse.Content.ReadFromJsonAsync<ApiPagedResponse<InboxConversationDto>>(JsonOptions);
        var row = Assert.Single(inbox!.Items);
        Assert.Equal(sent.ConversationId, row.ConversationId);
        Assert.Equal(aliceId, row.OtherParticipantId);
        Assert.Equal("API Unread Alice", row.OtherParticipantDisplayName);
        Assert.Equal("Hello from Alice", row.LastMessagePreview);
        Assert.True(row.HasUnread);
        Assert.Equal(1, row.UnreadCount);
        Assert.Equal($"/messages/{sent.ConversationId:D}", row.DetailPath);

        using var unreadResponse = await bobApi.GetAsync(MessagesApiEndpoints.UnreadCountPath);
        Assert.Contains("no-store", unreadResponse.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var unread = await unreadResponse.Content.ReadFromJsonAsync<UnreadConversationsDto>(JsonOptions);
        Assert.Equal(1, unread!.UnreadConversationCount);
        Assert.Equal(1, await service.CountUnreadConversationsAsync(bobId));

        var html = await bobWeb.GetStringAsync("/messages");
        Assert.Contains("API Unread Alice", html, StringComparison.Ordinal);
        Assert.Contains("1 unread", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Messages, 1 unread conversations\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetConversation_MarksRead_MatchingWebsiteOpen()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-read-alice@example.com",
            "API Read Alice",
            "api-read-bob@example.com",
            "API Read Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Please read this");
        var conversationId = sent.ConversationId!.Value;

        using var bobApi = CreateBearerClient(bobId, "API Read Bob", "api-read-bob@example.com");
        using var conversationResponse = await bobApi.GetAsync(MessagesApiEndpoints.ConversationPath(conversationId));

        Assert.Equal(HttpStatusCode.OK, conversationResponse.StatusCode);
        Assert.Contains("no-store", conversationResponse.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var detail = await conversationResponse.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(conversationId, detail!.ConversationId);
        Assert.Equal(aliceId, detail.OtherParticipantId);
        Assert.Equal("API Read Alice", detail.OtherParticipantDisplayName);
        Assert.Equal($"/messages/{conversationId:D}", detail.DetailPath);
        var message = Assert.Single(detail.Messages);
        Assert.Equal("Please read this", message.Body);
        Assert.False(message.IsMine);
        Assert.Equal(aliceId, message.SenderMemberId);
        Assert.True(detail.CanSendReply);

        Assert.Equal(0, await service.CountUnreadConversationsAsync(bobId));

        using var inboxResponse = await bobApi.GetAsync(MessagesApiEndpoints.Path);
        var inbox = await inboxResponse.Content.ReadFromJsonAsync<ApiPagedResponse<InboxConversationDto>>(JsonOptions);
        var row = Assert.Single(inbox!.Items);
        Assert.False(row.HasUnread);
        Assert.Equal(0, row.UnreadCount);

        using var unreadResponse = await bobApi.GetAsync(MessagesApiEndpoints.UnreadCountPath);
        var unread = await unreadResponse.Content.ReadFromJsonAsync<UnreadConversationsDto>(JsonOptions);
        Assert.Equal(0, unread!.UnreadConversationCount);

        using var bobWeb = CreateCookieClient(bobId, "API Read Bob", "api-read-bob@example.com");
        var html = await bobWeb.GetStringAsync("/messages");
        Assert.DoesNotContain("1 unread", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Messages\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetConversation_ReturnsNotFound_ForNonParticipantAndUnknownId()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-404-alice@example.com",
            "API 404 Alice",
            "api-404-bob@example.com",
            "API 404 Bob");
        var outsiderId = Guid.NewGuid();
        await SeedMemberAsync(outsiderId, "API 404 Outsider", "api-404-outsider@example.com");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Private");

        using var outsider = CreateBearerClient(outsiderId, "API 404 Outsider", "api-404-outsider@example.com");
        using var hidden = await outsider.GetAsync(MessagesApiEndpoints.ConversationPath(sent.ConversationId!.Value));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("application/problem+json", hidden.Content.Headers.ContentType?.MediaType);

        using var bob = CreateBearerClient(bobId, "API 404 Bob", "api-404-bob@example.com");
        using var missing = await bob.GetAsync(MessagesApiEndpoints.ConversationPath(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var problem = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Inbox_ClampsPaging_AndConversationDefaultsToLatestPage()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-page-alice@example.com",
            "API Page Alice",
            "api-page-bob@example.com",
            "API Page Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var created = await service.ComposeAsync(aliceId, bobId, "Msg 1");
        var conversationId = created.ConversationId!.Value;
        Assert.True((await service.ReplyAsync(conversationId, aliceId, "Msg 2")).Succeeded);
        Assert.True((await service.ReplyAsync(conversationId, aliceId, "Msg 3")).Succeeded);

        using var bob = CreateBearerClient(bobId, "API Page Bob", "api-page-bob@example.com");
        using var clamped = await bob.GetAsync($"{MessagesApiEndpoints.Path}?page=0&pageSize=1000");
        var inbox = await clamped.Content.ReadFromJsonAsync<ApiPagedResponse<InboxConversationDto>>(JsonOptions);
        Assert.Equal(1, inbox!.Page);
        Assert.Equal(PrivateMessageLimits.MaxInboxPageSize, inbox.PageSize);

        using var latest = await bob.GetAsync(
            $"{MessagesApiEndpoints.ConversationPath(conversationId)}?pageSize=2");
        var latestPage = await latest.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions);
        Assert.Equal(2, latestPage!.Page);
        Assert.Equal(2, latestPage.PageSize);
        Assert.Equal(3, latestPage.TotalCount);
        Assert.Equal(["Msg 2", "Msg 3"], latestPage.Messages.Select(item => item.Body).ToArray());

        using var first = await bob.GetAsync(
            $"{MessagesApiEndpoints.ConversationPath(conversationId)}?page=1&pageSize=2");
        var firstPage = await first.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions);
        Assert.Equal(1, firstPage!.Page);
        Assert.Equal(["Msg 1", "Msg 2"], firstPage.Messages.Select(item => item.Body).ToArray());
    }

    [Fact]
    public async Task Reply_RequiresMobileBearer_NotCookie()
    {
        using var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        using var cookieOnly = factory.CreateAnonymousClient(allowAutoRedirect: false);
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, Guid.NewGuid().ToString());
        cookieOnly.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Cookie Fan");

        foreach (var client in new[] { anonymous, cookieOnly })
        {
            using var response = await client.PostAsJsonAsync(
                MessagesApiEndpoints.ConversationPath(Guid.NewGuid()),
                new { body = "Should not send." });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Reply_UsesSameSortKeyOrderAsWebsitePost()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-reply-alice@example.com",
            "API Reply Alice",
            "api-reply-bob@example.com",
            "API Reply Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Msg 1");
        var conversationId = sent.ConversationId!.Value;
        var path = MessagesApiEndpoints.ConversationPath(conversationId);

        using var bobApi = CreateBearerClient(bobId, "API Reply Bob", "api-reply-bob@example.com");
        using var created = await bobApi.PostAsJsonAsync(path, new { body = "Msg 2" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal($"/messages/{conversationId:D}", created.Headers.Location?.OriginalString);
        Assert.Contains("no-store", created.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var afterAppReply = await created.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions);
        Assert.True(afterAppReply!.CanSendReply);
        Assert.Equal(["Msg 1", "Msg 2"], afterAppReply.Messages.Select(item => item.Body).ToArray());
        Assert.True(afterAppReply.Messages[0].SortKey < afterAppReply.Messages[1].SortKey);
        Assert.True(afterAppReply.Messages[1].IsMine);

        using var aliceWeb = CreateCookieClient(aliceId, "API Reply Alice", "api-reply-alice@example.com");
        var conversationHtml = await aliceWeb.GetStringAsync($"/messages/{conversationId:D}");
        using var webReply = await aliceWeb.PostAsync(
            $"/messages/{conversationId:D}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(conversationHtml),
                ["Input.Body"] = "Msg 3",
            }));
        Assert.Equal(HttpStatusCode.Redirect, webReply.StatusCode);

        using var latest = await bobApi.GetAsync(path);
        var detail = await latest.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions);
        Assert.Equal(["Msg 1", "Msg 2", "Msg 3"], detail!.Messages.Select(item => item.Body).ToArray());
        Assert.True(detail.Messages[0].SortKey < detail.Messages[1].SortKey);
        Assert.True(detail.Messages[1].SortKey < detail.Messages[2].SortKey);
        Assert.False(detail.Messages[2].IsMine);

        var html = await aliceWeb.GetStringAsync($"/messages/{conversationId:D}");
        Assert.Contains("Msg 1", html, StringComparison.Ordinal);
        Assert.Contains("Msg 2", html, StringComparison.Ordinal);
        Assert.Contains("Msg 3", html, StringComparison.Ordinal);
        var first = html.IndexOf("Msg 1", StringComparison.Ordinal);
        var second = html.IndexOf("Msg 2", StringComparison.Ordinal);
        var third = html.IndexOf("Msg 3", StringComparison.Ordinal);
        Assert.True(first < second && second < third);
    }

    [Fact]
    public async Task Reply_ReturnsNotFound_ForNonParticipant()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-reply-404-alice@example.com",
            "API Reply 404 Alice",
            "api-reply-404-bob@example.com",
            "API Reply 404 Bob");
        var outsiderId = Guid.NewGuid();
        await SeedMemberAsync(outsiderId, "API Reply 404 Outsider", "api-reply-404-outsider@example.com");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Private");

        using var outsider = CreateBearerClient(outsiderId, "API Reply 404 Outsider", "api-reply-404-outsider@example.com");
        using var hidden = await outsider.PostAsJsonAsync(
            MessagesApiEndpoints.ConversationPath(sent.ConversationId!.Value),
            new { body = "Intruder" });
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("application/problem+json", hidden.Content.Headers.ContentType?.MediaType);

        using var bob = CreateBearerClient(bobId, "API Reply 404 Bob", "api-reply-404-bob@example.com");
        using var missing = await bob.PostAsJsonAsync(
            MessagesApiEndpoints.ConversationPath(Guid.NewGuid()),
            new { body = "Gone" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var problem = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
        Assert.Equal("Conversation was not found.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Reply_RejectsEmptyAndOversizedBody()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-reply-body-alice@example.com",
            "API Reply Body Alice",
            "api-reply-body-bob@example.com",
            "API Reply Body Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Hello");
        var path = MessagesApiEndpoints.ConversationPath(sent.ConversationId!.Value);
        using var bob = CreateBearerClient(bobId, "API Reply Body Bob", "api-reply-body-bob@example.com");

        using var empty = await bob.PostAsJsonAsync(path, new { body = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        var emptyProblem = await empty.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Message body is required.", emptyProblem.GetProperty("detail").GetString());

        using var missingBody = await bob.PostAsync(path, new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, missingBody.StatusCode);

        using var tooLong = await bob.PostAsJsonAsync(
            path,
            new { body = new string('a', PrivateMessageLimits.MaxBodyLength + 1) });
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        var longProblem = await tooLong.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("4000", longProblem.GetProperty("detail").GetString(), StringComparison.Ordinal);

        using var noJson = await bob.PostAsync(path, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, noJson.StatusCode);
        var noJsonProblem = await noJson.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("A JSON body is required.", noJsonProblem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Reply_ReturnsForbidden_WhenMessagingIsBlocked()
    {
        var (aliceId, bobId) = await SeedConversationPairAsync(
            "api-reply-block-alice@example.com",
            "API Reply Block Alice",
            "api-reply-block-bob@example.com",
            "API Reply Block Bob");
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Hello");
        var conversationId = sent.ConversationId!.Value;
        Assert.True((await service.BlockAsync(aliceId, bobId)).Succeeded);

        using var bob = CreateBearerClient(bobId, "API Reply Block Bob", "api-reply-block-bob@example.com");
        using var get = await bob.GetAsync(MessagesApiEndpoints.ConversationPath(conversationId));
        var detail = await get.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions);
        Assert.False(detail!.CanSendReply);

        using var reply = await bob.PostAsJsonAsync(
            MessagesApiEndpoints.ConversationPath(conversationId),
            new { body = "Should fail" });
        Assert.Equal(HttpStatusCode.Forbidden, reply.StatusCode);
        var problem = await reply.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(PrivateMessageService.UnableToSendMessage, problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Reply_ReturnsTooManyRequests_AfterSharedRateLimit()
    {
        await using var limitedFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.Configure<PrivateMessageRateLimitOptions>(opts =>
            {
                opts.MaxMessagesPerWindow = 1;
                opts.NewAccountMaxMessagesPerWindow = 1;
            });
        });

        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        await SeedMemberAsync(limitedFactory, aliceId, "API Reply Limit Alice", "api-reply-limit-alice@example.com");
        await SeedMemberAsync(limitedFactory, bobId, "API Reply Limit Bob", "api-reply-limit-bob@example.com");
        var service = limitedFactory.Services.GetRequiredService<PrivateMessageService>();
        var sent = await service.ComposeAsync(aliceId, bobId, "Msg 1");
        var path = MessagesApiEndpoints.ConversationPath(sent.ConversationId!.Value);
        using var alice = CreateBearerClient(
            limitedFactory,
            aliceId,
            "API Reply Limit Alice",
            "api-reply-limit-alice@example.com");

        using var response = await alice.PostAsJsonAsync(path, new { body = "Msg 2" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem.GetProperty("status").GetInt32());
        Assert.Equal(PrivateMessageService.RateLimitedMessage, problem.GetProperty("detail").GetString());
    }

    [Fact]
    public void Mapper_CopiesInboxAndConversationFields()
    {
        var conversationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var otherId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var at = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var inbox = MessagesApiMapper.ToInboxItem(
            new PrivateConversationListItem(
                conversationId,
                otherId,
                "Roger",
                "See you at Wembley",
                at,
                true,
                2));

        Assert.Equal(conversationId, inbox.ConversationId);
        Assert.Equal(otherId, inbox.OtherParticipantId);
        Assert.Equal("Roger", inbox.OtherParticipantDisplayName);
        Assert.Equal("See you at Wembley", inbox.LastMessagePreview);
        Assert.Equal(at, inbox.LastMessageAt);
        Assert.True(inbox.HasUnread);
        Assert.Equal(2, inbox.UnreadCount);
        Assert.Equal("/messages/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", inbox.DetailPath);
        Assert.Equal(inbox.DetailPath, Assert.Single(MessagesApiMapper.ToInboxItems(
            [new PrivateConversationListItem(conversationId, otherId, "Roger", "See you at Wembley", at, true, 2)])).DetailPath);

        var messageId = Guid.NewGuid();
        var detail = MessagesApiMapper.ToConversation(
            new PrivateConversationDetail(
                conversationId,
                otherId,
                "Roger",
                [new PrivateMessageItem(messageId, otherId, "Roger", "See you at Wembley", at, false, 9)],
                TotalCount: 1,
                Page: 1,
                PageSize: 50),
            canSendReply: true);

        Assert.Equal(conversationId, detail.ConversationId);
        Assert.Equal(1, detail.TotalCount);
        Assert.Equal(1, detail.Page);
        Assert.Equal(50, detail.PageSize);
        var message = Assert.Single(detail.Messages);
        Assert.Equal(messageId, message.Id);
        Assert.Equal(otherId, message.SenderMemberId);
        Assert.Equal("Roger", message.SenderDisplayName);
        Assert.Equal("See you at Wembley", message.Body);
        Assert.False(message.IsMine);
        Assert.Equal(9, message.SortKey);
        Assert.True(detail.CanSendReply);
        Assert.Equal("/messages/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", detail.DetailPath);

        var blocked = MessagesApiMapper.ToConversation(
            new PrivateConversationDetail(
                conversationId,
                otherId,
                "Roger",
                [new PrivateMessageItem(messageId, otherId, "Roger", "See you at Wembley", at, false, 9)],
                TotalCount: 1,
                Page: 1,
                PageSize: 50),
            canSendReply: false);
        Assert.False(blocked.CanSendReply);
    }

    [Fact]
    public void Mapper_UsesWebsiteConversationPath()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.Equal("/messages/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", MessagesApiMapper.ConversationDetailPath(id));
    }

    private async Task<(Guid AliceId, Guid BobId)> SeedConversationPairAsync(
        string aliceEmail,
        string aliceName,
        string bobEmail,
        string bobName)
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        await SeedMemberAsync(aliceId, aliceName, aliceEmail);
        await SeedMemberAsync(bobId, bobName, bobEmail);
        return (aliceId, bobId);
    }

    private HttpClient CreateBearerClient(Guid memberId, string displayName, string email) =>
        CreateBearerClient(factory, memberId, displayName, email);

    private static HttpClient CreateBearerClient(
        QueenZoneWebApplicationFactory source,
        Guid memberId,
        string displayName,
        string email)
    {
        using var scope = source.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<MobileAuthTokenIssuer>();
        var token = issuer.IssueAccessToken(memberId, email, displayName);
        var client = source.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateCookieClient(Guid memberId, string displayName, string email)
    {
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, memberId.ToString());
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, displayName);
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.EmailHeader, email);
        return client;
    }

    private Task SeedMemberAsync(Guid memberId, string displayName, string email) =>
        SeedMemberAsync(factory, memberId, displayName, email);

    private static async Task SeedMemberAsync(
        QueenZoneWebApplicationFactory source,
        Guid memberId,
        string displayName,
        string email)
    {
        using var scope = source.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>();
        await repository.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            """(?:name=["']__RequestVerificationToken["'][^>]*value=["'](?<token>[^"']+)["'])|(?:value=["'](?<token>[^"']+)["'][^>]*name=["']__RequestVerificationToken["'])""",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }
}
