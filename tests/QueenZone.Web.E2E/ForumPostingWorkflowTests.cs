using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace QueenZone.Web.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ForumPostingWorkflowTests : E2EPageTest
{
    private static readonly Regex NewTopicUrl = new(".*/forum/topic/\\d+/playwright-forum-topic-.*", RegexOptions.IgnoreCase);
    private const string TestMemberIdHeader = "X-Test-Member-Id";
    private const string TestMemberNameHeader = "X-Test-Member-Name";
    private const string TestMemberName = "Playwright Forum Fan";

    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            BaseURL = BaseUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                [TestMemberIdHeader] = Guid.NewGuid().ToString(),
                [TestMemberNameHeader] = TestMemberName,
            }
        };

    [Test]
    public async Task MemberCanCreateTopicAndReply()
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var subject = $"Playwright forum topic {stamp}";
        var firstBody = $"Playwright first post {stamp}";
        var replyBody = $"Playwright reply {stamp}";

        await Page.GotoAsync("/forum/1/the-music");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New thread" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "New thread" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "New thread", Level = 1 })).ToBeVisibleAsync();

        await Page.GetByLabel("Subject").FillAsync(subject);
        await FillRichTextEditorAsync(firstBody);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create thread" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(NewTopicUrl);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = subject, Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.Locator(".qz-forum-post").Filter(new() { HasText = firstBody })).ToBeVisibleAsync();
        await Expect(Page.GetByText(TestMemberName)).ToBeVisibleAsync();

        await FillRichTextEditorAsync(replyBody);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Reply" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/forum/topic/\\d+/playwright-forum-topic-.*#post-\\d+", RegexOptions.IgnoreCase));
        await Expect(Page.Locator(".qz-forum-post").Filter(new() { HasText = replyBody })).ToBeVisibleAsync();
    }

    private async Task FillRichTextEditorAsync(string text)
    {
        var editor = Page.Locator(".ql-editor").Last;
        await Expect(editor).ToBeVisibleAsync();
        await editor.ClickAsync();
        await Page.Keyboard.InsertTextAsync(text);
    }
}
