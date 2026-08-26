using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PushNotificationPayloadTests
{
    [Fact]
    public void ForumReply_IncludesTopicAndPost()
    {
        var payload = PushNotificationPayload.ForumReply(12, 34, "  Ranking every studio album  ");

        Assert.Equal(NotificationCategoryNames.ForumReply, payload.Category);
        Assert.Equal("Ranking every studio album", payload.Title);
        Assert.Equal("12", payload.Data["topicId"]);
        Assert.Equal("34", payload.Data["postId"]);
        Assert.Equal("forumReply", payload.Data["category"]);
    }

    [Fact]
    public void PrivateMessage_IncludesConversationId()
    {
        var conversationId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var payload = PushNotificationPayload.PrivateMessage(conversationId);

        Assert.Equal(NotificationCategoryNames.PrivateMessage, payload.Category);
        Assert.Equal(conversationId.ToString(), payload.Data["conversationId"]);
        Assert.DoesNotContain("token", payload.Data.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void News_IncludesArticleId_AndTruncatesTitle()
    {
        var longTitle = new string('A', 200);
        var payload = PushNotificationPayload.News(88, longTitle);

        Assert.Equal("news", payload.Category);
        Assert.Equal("88", payload.Data["articleId"]);
        Assert.Equal(PushNotificationPayload.MaxAlertLength, payload.Title.Length);
    }

    [Fact]
    public async Task EmptyTopicWatchLookup_AlwaysEmpty()
    {
        var lookup = new EmptyTopicWatchLookup();
        Assert.Empty(await lookup.ListMemberIdsAsync(1002));
    }
}
