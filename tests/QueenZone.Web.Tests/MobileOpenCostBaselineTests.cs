using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using Xunit.Abstractions;

namespace QueenZone.Web.Tests;

/// <summary>
/// Issue #764 baseline: the GETs <c>ThreadScreen</c> and <c>fetchConversation</c>
/// already make, measured cold then warm on a fresh Testing host. Numbers for
/// <c>docs/architecture/hosting-scale-and-cache.md</c>.
/// </summary>
public sealed class MobileOpenCostBaselineTests
{
    public const int BusyTopicId = 1002;

    public const int ThreadPostsPageSize = ForumRoutes.PostsPageSize;

    public const int ConversationOpenPageSize = PrivateMessageLimits.ConversationPageSize;

    /// <summary>
    /// Seeded Testing thread 1002 header JSON is stable (no request-scoped ids).
    /// Measured 2026-08-30 on a fresh Testing host.
    /// </summary>
    public const int BusyTopicJsonBytes = 227;

    /// <summary>
    /// Seeded Testing thread 1002 posts page 1 (15 items, two attachments).
    /// Measured 2026-08-30 on a fresh Testing host.
    /// </summary>
    public const int BusyTopicPostsPage1JsonBytes = 4617;

    /// <summary>
    /// Watch payload is <c>{"watching":false}</c> — no request-scoped ids.
    /// </summary>
    public const int WatchJsonBytes = 18;

    /// <summary>
    /// Problem Details for the defensive poll GET on sample topic 1002 (hasPoll is not false).
    /// </summary>
    public const int BusyTopicPollNotFoundJsonBytes = 211;

    private readonly ITestOutputHelper output;

    public MobileOpenCostBaselineTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task Signed_in_thread_and_conversation_open_match_measured_baseline()
    {
        await using var factory = new QueenZoneWebApplicationFactory();
        var aliceId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var bobId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        await SeedMemberAsync(factory, aliceId, "Open Cost Alice", "open-cost-alice@example.test");
        await SeedMemberAsync(factory, bobId, "Open Cost Bob", "open-cost-bob@example.test");
        var conversationId = await SeedConversationAsync(factory, aliceId, bobId);

        using var client = CreateBearerClient(factory, bobId, "Open Cost Bob", "open-cost-bob@example.test");

        var threadPaths = new[]
        {
            $"{ForumApiEndpoints.RootPath}/topics/{BusyTopicId}",
            $"{ForumApiEndpoints.RootPath}/topics/{BusyTopicId}/posts?page=1&pageSize={ThreadPostsPageSize}",
            $"{ForumApiEndpoints.RootPath}/topics/{BusyTopicId}/watch",
            $"{ForumApiEndpoints.RootPath}/topics/{BusyTopicId}/poll",
        };

        var conversationPath =
            $"{MessagesApiEndpoints.ConversationPath(conversationId)}?pageSize={ConversationOpenPageSize}";

        var threadCold = await MeasureAsync(client, threadPaths);
        var threadWarm = await MeasureAsync(client, threadPaths);
        var conversationCold = await MeasureAsync(client, [conversationPath]);
        var conversationWarm = await MeasureAsync(client, [conversationPath]);

        WriteTable("thread open (signed-in, topic 1002)", threadCold, threadWarm);
        WriteTable("conversation open (12-message seed)", conversationCold, conversationWarm);

        Assert.Equal(4, threadCold.Count);
        Assert.Equal(4, threadWarm.Count);
        var conversationColdProbe = Assert.Single(conversationCold);
        var conversationWarmProbe = Assert.Single(conversationWarm);

        AssertProbe(threadCold[0], HttpStatusCode.OK, expectNoStore: false, BusyTopicJsonBytes);
        AssertProbe(threadWarm[0], HttpStatusCode.OK, expectNoStore: false, BusyTopicJsonBytes);
        AssertProbe(threadCold[1], HttpStatusCode.OK, expectNoStore: false, BusyTopicPostsPage1JsonBytes);
        AssertProbe(threadWarm[1], HttpStatusCode.OK, expectNoStore: false, BusyTopicPostsPage1JsonBytes);
        AssertProbe(threadCold[2], HttpStatusCode.OK, expectNoStore: true, WatchJsonBytes);
        AssertProbe(threadWarm[2], HttpStatusCode.OK, expectNoStore: true, WatchJsonBytes);
        AssertProbe(threadCold[3], HttpStatusCode.NotFound, expectNoStore: false, BusyTopicPollNotFoundJsonBytes);
        AssertProbe(threadWarm[3], HttpStatusCode.NotFound, expectNoStore: false, BusyTopicPollNotFoundJsonBytes);

        using var topicDoc = JsonDocument.Parse(threadCold[0].Body);
        Assert.False(topicDoc.RootElement.TryGetProperty("hasPoll", out var hasPoll)
            && hasPoll.ValueKind is JsonValueKind.False);
        Assert.Equal(26, topicDoc.RootElement.GetProperty("postCount").GetInt32());

        AssertProbe(conversationColdProbe, HttpStatusCode.OK, expectNoStore: true);
        AssertProbe(conversationWarmProbe, HttpStatusCode.OK, expectNoStore: true);
        Assert.Equal(conversationColdProbe.JsonBytes, conversationWarmProbe.JsonBytes);
        Assert.InRange(conversationColdProbe.JsonBytes, 3_800, 4_400);

        using var conversationDoc = JsonDocument.Parse(conversationColdProbe.Body);
        Assert.Equal(12, conversationDoc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(12, conversationDoc.RootElement.GetProperty("messages").GetArrayLength());
        Assert.Equal(ConversationOpenPageSize, conversationDoc.RootElement.GetProperty("pageSize").GetInt32());
    }

    private static async Task<IReadOnlyList<OpenCostProbe>> MeasureAsync(
        HttpClient client,
        IReadOnlyList<string> paths)
    {
        var probes = new List<OpenCostProbe>(paths.Count);
        foreach (var path in paths)
        {
            using var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            probes.Add(new OpenCostProbe(
                path,
                response.StatusCode,
                Encoding.UTF8.GetByteCount(body),
                body,
                response.Headers.CacheControl?.ToString(),
                response.Headers.ETag?.ToString(),
                response.Headers.Age?.ToString(),
                response.Headers.Contains("X-Cache") || response.Headers.Contains("x-cache")));
        }

        return probes;
    }

    private static void AssertProbe(
        OpenCostProbe probe,
        HttpStatusCode status,
        bool expectNoStore,
        int? expectedJsonBytes = null)
    {
        Assert.Equal(status, probe.StatusCode);
        AssertMiss(probe, expectNoStore);
        if (expectedJsonBytes is int expected)
        {
            Assert.Equal(expected, probe.JsonBytes);
        }
    }

    private static void AssertMiss(OpenCostProbe probe, bool expectNoStore)
    {
        var cacheControl = probe.CacheControl ?? string.Empty;
        if (expectNoStore)
        {
            Assert.Contains("no-store", cacheControl, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.DoesNotContain("max-age", cacheControl, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("public", cacheControl, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(string.IsNullOrEmpty(probe.ETag));
        Assert.True(string.IsNullOrEmpty(probe.Age));
        Assert.False(probe.HasXCache);
    }

    private void WriteTable(
        string title,
        IReadOnlyList<OpenCostProbe> cold,
        IReadOnlyList<OpenCostProbe> warm)
    {
        output.WriteLine($"## {title}");
        output.WriteLine("| GET | Status | Cold JSON bytes | Warm JSON bytes | Cache-Control | ETag | Age / X-Cache | Existing cache |");
        output.WriteLine("| --- | --- | ---: | ---: | --- | --- | --- | --- |");
        for (var i = 0; i < cold.Count; i++)
        {
            var c = cold[i];
            var w = warm[i];
            output.WriteLine(
                $"| `{c.Path}` | {(int)c.StatusCode} | {c.JsonBytes} | {w.JsonBytes} | {c.CacheControl ?? "(none)"} | {c.ETag ?? "(none)"} | {(c.Age ?? "(none)")}/{(c.HasXCache ? "hit" : "none")} | miss |");
        }

        output.WriteLine($"Thread/conversation request count: {cold.Count} cold, {warm.Count} warm.");
    }

    private static HttpClient CreateBearerClient(
        QueenZoneWebApplicationFactory factory,
        Guid memberId,
        string displayName,
        string email)
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<MobileAuthTokenIssuer>();
        var token = issuer.IssueAccessToken(memberId, email, displayName);
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task SeedMemberAsync(
        QueenZoneWebApplicationFactory factory,
        Guid memberId,
        string displayName,
        string email)
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

    private static async Task<Guid> SeedConversationAsync(
        QueenZoneWebApplicationFactory factory,
        Guid aliceId,
        Guid bobId)
    {
        var service = factory.Services.GetRequiredService<PrivateMessageService>();
        var first = await service.ComposeAsync(aliceId, bobId, "Did you finish the studio album ranking?");
        Assert.True(first.Succeeded);
        var conversationId = first.ConversationId!.Value;

        string[] replies =
        [
            "Almost — Opera is locked at one. SHA is fighting Jazz for two.",
            "Sheer Heart Attack for raw energy, then Opera. Jazz is overrated here.",
            "Jazz has the best deep cuts. Let Me Live still gets skipped too often.",
            "Fair. Night at the Opera side two is basically perfect though.",
            "Agreed. Prophet's Song into Love of My Life is unfair.",
            "Where do you put The Game? Another One Bites the Dust crowds out Dragon Attack.",
            "The Game is mid-pack. Save Me and Sail Away Sweet Sister carry it.",
            "Innuendo above The Miracle for me. The Show Must Go On closes it.",
            "Miracle has I Want It All. That chorus still fills a room.",
            "Put Innuendo at three and Miracle at five. News of the World stays four.",
            "Done. I'll post the full list in the ranking thread after lunch.",
        ];

        var sender = bobId;
        foreach (var body in replies)
        {
            var sent = await service.ReplyAsync(conversationId, sender, body);
            Assert.True(sent.Succeeded);
            sender = sender == bobId ? aliceId : bobId;
        }

        return conversationId;
    }

    private sealed record OpenCostProbe(
        string Path,
        HttpStatusCode StatusCode,
        int JsonBytes,
        string Body,
        string? CacheControl,
        string? ETag,
        string? Age,
        bool HasXCache);
}
