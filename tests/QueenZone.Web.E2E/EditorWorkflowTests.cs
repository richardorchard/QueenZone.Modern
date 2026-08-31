using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace QueenZone.Web.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(E2ECategories.Deterministic)]
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

    [Test]
    public async Task AdminCanCropUploadedImageAndSaveArticle()
    {
        await GotoAdminAsync("/admin/news/new");
        await Page.GetByLabel("Title").FillAsync($"Cropped image browser test {Guid.NewGuid():N}");
        await Page.GetByLabel("Excerpt").FillAsync("Browser coverage for the cropped image save path.");
        await Page.Locator(".ql-editor").FillAsync("Cropped image browser test body.");

        var imagePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "design", "crest.jpg"));
        await Page.GetByLabel("Article image").SetInputFilesAsync(imagePath);
        await Expect(Page.Locator("[data-article-image-dialog]")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Use this crop" }).ClickAsync();

        var zoom = Page.Locator("[data-article-image-zoom]");
        await Expect(zoom).ToBeDisabledAsync();
        Assert.That(await Page.Locator("form.admin-form").EvaluateAsync<bool>("form => form.checkValidity()"), Is.True);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/admin/news/\\d+/edit"));

        await Page.GetByLabel("Article image").SetInputFilesAsync(imagePath);
        await Expect(Page.Locator("[data-article-image-dialog]")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Use this crop" }).ClickAsync();
        await Expect(Page.Locator("[data-article-image-zoom]")).ToBeDisabledAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/admin/news/\\d+/edit"));
        await Expect(Page.GetByText("Article saved.")).ToBeVisibleAsync();
    }

    private async Task GotoAdminAsync(string path)
    {
        var response = await Page.GotoAsync(path);
        Assert.That(response?.Status, Is.EqualTo(200), $"Expected {path} to load as admin user {AdminEmail}.");
    }
}
