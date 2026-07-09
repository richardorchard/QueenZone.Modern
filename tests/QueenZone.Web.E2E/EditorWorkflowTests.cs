using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace QueenZone.Web.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class EditorWorkflowTests : E2EPageTest
{
    // Must match SampleNewsDiscoveryData seed titles used by AddQueenZoneInMemoryData.
    private const string SeededSourceTitle = "E2E editorial workflow source item";
    private const string SeededDraftTitle = "E2E editorial workflow draft";

    private static string AdminEmail =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@test.local";

    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            BaseURL = BaseUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                [TestAuthHeaderName] = AdminEmail
            }
        };

    private const string TestAuthHeaderName = "X-Test-User-Email";

    [Test]
    public async Task AdminCanPromoteDiscoveryDraftPublishAndSeeItPublicly()
    {
        await GotoAdminAsync("/admin/news-discovery");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "News discovery review", Level = 1 }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = SeededSourceTitle }))
            .ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Row)
            .Filter(new() { HasText = SeededSourceTitle })
            .GetByRole(AriaRole.Link, new() { Name = "Review" })
            .ClickAsync();
        await Expect(Page.Locator("h1")).ToContainTextAsync("Review candidate #");
        await Expect(Page.GetByText(SeededDraftTitle)).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Promote to admin news" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/admin/news/\\d+/edit"));
        await Expect(Page.GetByLabel("Title")).ToHaveValueAsync(SeededDraftTitle);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/admin/news/?$"));
        await Expect(Page.GetByText(SeededDraftTitle)).ToBeVisibleAsync();

        await Page.GotoAsync("/news");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "News", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = SeededDraftTitle })).ToBeVisibleAsync();
    }

    private async Task GotoAdminAsync(string path)
    {
        var response = await Page.GotoAsync(path);
        Assert.That(response?.Status, Is.EqualTo(200), $"Expected {path} to load as admin user {AdminEmail}.");
    }
}
