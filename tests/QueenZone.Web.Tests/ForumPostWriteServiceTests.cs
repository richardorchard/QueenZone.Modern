using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Web.Tests;

public sealed class ForumPostWriteServiceTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ForumPostWriteServiceTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateTopic_rejects_missing_board_before_rate_limit()
    {
        var outcome = await CreateTopicAsync(Guid.NewGuid(), 9999, "Valid title here", "Hello");

        Assert.Equal(ForumWriteStatus.CategoryNotFound, outcome.Status);
        Assert.False(outcome.Succeeded);
    }

    [Fact]
    public async Task CreateTopic_rejects_short_title_and_empty_body()
    {
        var outcome = await CreateTopicAsync(Guid.NewGuid(), 1, "Hi", "<script>alert(1)</script>");

        Assert.Equal(ForumWriteStatus.ValidationFailed, outcome.Status);
        Assert.Contains(outcome.FieldErrors, error => error.Field == "Subject");
        Assert.Contains(outcome.FieldErrors, error => error.Field == "Body");
    }

    [Fact]
    public async Task CreateReply_rejects_missing_and_empty_body()
    {
        var missing = await CreateReplyAsync(Guid.NewGuid(), 9999, "Hello");
        Assert.Equal(ForumWriteStatus.TopicNotFound, missing.Status);

        var empty = await CreateReplyAsync(Guid.NewGuid(), 1002, "   ");
        Assert.Equal(ForumWriteStatus.ValidationFailed, empty.Status);
        Assert.Equal(ForumPostWriteService.BodyRequiredMessage, empty.FieldErrors[0].Message);
    }

    [Fact]
    public async Task CreateTopic_persists_plain_text_as_wrapped_html()
    {
        var memberId = Guid.NewGuid();
        var outcome = await CreateTopicAsync(memberId, 1, "Wrapped storage thread", "Hello from unit test");

        Assert.True(outcome.Succeeded);
        Assert.Equal("<p>Hello from unit test</p>", outcome.SanitizedBody);
        Assert.True(outcome.TopicId > 0);
        Assert.True(outcome.PostId > 0);
    }

    [Fact]
    public async Task CreateTopic_rejects_disallowed_attachment_types()
    {
        using var stream = new MemoryStream("not-an-image"u8.ToArray());
        IFormFile file = new FormFile(stream, 0, stream.Length, "attachments", "malware.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/x-msdownload",
        };

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ForumPostWriteService>();
        var outcome = await service.CreateTopicAsync(
            Guid.NewGuid(),
            "Service Tester",
            1,
            "Attachment rejection thread",
            "Body with a bad attachment",
            [file],
            poll: null);

        Assert.Equal(ForumWriteStatus.ValidationFailed, outcome.Status);
        Assert.Contains(outcome.FieldErrors, error =>
            error.Field == "Attachments" && error.Message.Contains("not allowed", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ForumWriteOutcome> CreateTopicAsync(
        Guid memberId,
        int categoryId,
        string title,
        string body)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ForumPostWriteService>();
        return await service.CreateTopicAsync(
            memberId,
            "Service Tester",
            categoryId,
            title,
            body,
            attachments: null,
            poll: null);
    }

    private async Task<ForumWriteOutcome> CreateReplyAsync(Guid memberId, int topicId, string body)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ForumPostWriteService>();
        return await service.CreateReplyAsync(
            memberId,
            "Service Tester",
            topicId,
            body,
            attachments: null);
    }
}
