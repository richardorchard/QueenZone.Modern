using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscussionComposerTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public NewsDiscussionComposerTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task List_BatchesReplyCounts_WithoutBodies_AndOmitsDiscussionWhenTopicMissing()
    {
        var lookup = new RecordingDiscussionLookup();
        lookup.ReplyCounts[11] = 4;
        lookup.ReplyCounts[12] = 0;
        var composer = new NewsDiscussionComposer(lookup);
        var items = new List<NewsItem>
        {
            Item(1, topicId: 11),
            Item(2, topicId: 12),
            Item(3, topicId: null),
        };

        var list = await composer.ToListItemsAsync(items);

        Assert.Equal([11, 12], Assert.Single(lookup.ReplyCountCalls));
        Assert.Equal(11, list[0].TopicId);
        Assert.Equal(4, list[0].ReplyCount);
        Assert.Equal(12, list[1].TopicId);
        Assert.Equal(0, list[1].ReplyCount);
        Assert.Null(list[2].TopicId);
        Assert.Null(list[2].ReplyCount);
        Assert.Empty(lookup.DiscussionCalls);
    }

    [Fact]
    public async Task Detail_ReturnsLastTwoReplies_NotOpeningPost()
    {
        var lookup = new RecordingDiscussionLookup
        {
            Discussion = (
                3,
                [
                    new NewsDiscussionPreview("Alice", new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc), "first reply"),
                    new NewsDiscussionPreview("Bob", new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc), "second reply"),
                ]),
        };
        var composer = new NewsDiscussionComposer(lookup);

        var detail = await composer.ToDetailAsync(Item(9, topicId: 77));

        Assert.Equal(77, detail.TopicId);
        Assert.Equal(3, detail.DiscussionReplyCount);
        Assert.Equal(2, detail.DiscussionPreview!.Count);
        Assert.Equal("Alice", detail.DiscussionPreview[0].AuthorDisplayName);
        Assert.Equal("Bob", detail.DiscussionPreview[1].AuthorDisplayName);
        Assert.DoesNotContain(
            detail.DiscussionPreview,
            preview => preview.Excerpt.Contains("opening", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(77, Assert.Single(lookup.DiscussionCalls));
        Assert.Equal(NewsForumDiscussion.PreviewReplyCount, lookup.LastPreviewCount);

        var website = await composer.ToDetailItemAsync(Item(9, topicId: 77));
        Assert.Equal(77, website.TopicId);
        Assert.Equal(3, website.DiscussionReplyCount);
        Assert.Equal(2, website.DiscussionPreview!.Count);
    }

    [Fact]
    public async Task Detail_WithoutTopicId_HasNoDiscussionBlock()
    {
        var lookup = new RecordingDiscussionLookup();
        var composer = new NewsDiscussionComposer(lookup);

        var detail = await composer.ToDetailAsync(Item(5, topicId: null));
        var website = await composer.ToDetailItemAsync(Item(5, topicId: null));

        Assert.Null(detail.TopicId);
        Assert.Null(detail.DiscussionReplyCount);
        Assert.Null(detail.DiscussionPreview);
        Assert.Null(website.TopicId);
        Assert.Null(website.DiscussionReplyCount);
        Assert.Null(website.DiscussionPreview);
        Assert.Empty(lookup.DiscussionCalls);
        Assert.Empty(lookup.ReplyCountCalls);
    }

    [Fact]
    public async Task ApiDetail_AfterFirstPublish_ExposesTopicAndLastTwoReplies()
    {
        using var scope = factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
        var write = scope.ServiceProvider.GetRequiredService<AdminNewsWriteService>();
        var forumWrite = scope.ServiceProvider.GetRequiredService<ForumPostWriteService>();
        var draft = await admin.CreateDraftAsync(
            new AdminNewsDraft("API discussion article", null, "Excerpt", "Body", DateTime.UtcNow.Date, null),
            "editor@test.local");
        var article = (await admin.GetByIdAsync(draft))!;
        await write.PublishAsync(article, "editor@test.local");
        var published = (await admin.GetByIdAsync(draft))!;
        Assert.NotNull(published.ForumTopicId);

        await forumWrite.CreateReplyAsync(
            Guid.NewGuid(), "First", published.ForumTopicId!.Value, "First reply body", attachments: null);
        await forumWrite.CreateReplyAsync(
            Guid.NewGuid(), "Second", published.ForumTopicId.Value, "Second reply body", attachments: null);
        await forumWrite.CreateReplyAsync(
            Guid.NewGuid(), "Third", published.ForumTopicId.Value, "Third reply body", attachments: null);

        using var client = factory.CreateAnonymousClient();
        using var detailResponse = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/{draft}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<NewsDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(published.ForumTopicId, detail!.TopicId);
        Assert.Equal(3, detail.DiscussionReplyCount);
        Assert.Equal(2, detail.DiscussionPreview!.Count);
        Assert.Equal("Second", detail.DiscussionPreview[0].AuthorDisplayName);
        Assert.Equal("Third", detail.DiscussionPreview[1].AuthorDisplayName);

        using var listResponse = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?page=1&pageSize=20");
        var list = await listResponse.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        var card = Assert.Single(list!.Items, item => item.Id == draft);
        Assert.Equal(published.ForumTopicId, card.TopicId);
        Assert.Equal(3, card.ReplyCount);
    }

    private static NewsItem Item(int id, int? topicId) =>
        new(
            id,
            $"Title {id}",
            "Excerpt",
            "Body",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            null,
            true,
            ForumTopicId: topicId);

    private sealed class RecordingDiscussionLookup : INewsForumDiscussionLookup
    {
        public Dictionary<int, int> ReplyCounts { get; } = [];

        public List<IReadOnlyList<int>> ReplyCountCalls { get; } = [];

        public List<int> DiscussionCalls { get; } = [];

        public int LastPreviewCount { get; private set; }

        public (int ReplyCount, IReadOnlyList<NewsDiscussionPreview> Preview) Discussion { get; set; } =
            (0, []);

        public Task<IReadOnlyDictionary<int, int>> GetReplyCountsAsync(
            IReadOnlyList<int> topicIds,
            CancellationToken cancellationToken = default)
        {
            ReplyCountCalls.Add(topicIds.ToList());
            return Task.FromResult<IReadOnlyDictionary<int, int>>(
                topicIds.ToDictionary(id => id, id => ReplyCounts.GetValueOrDefault(id)));
        }

        public Task<(int ReplyCount, IReadOnlyList<NewsDiscussionPreview> Preview)> GetDiscussionAsync(
            int topicId,
            int previewCount,
            CancellationToken cancellationToken = default)
        {
            DiscussionCalls.Add(topicId);
            LastPreviewCount = previewCount;
            return Task.FromResult(Discussion);
        }
    }
}
