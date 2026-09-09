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
        // Use a link locator, not GetByText: the row's delete-confirmation dialog also
        // names the article in its body text, which would make a plain text locator
        // resolve to two elements even while the dialog is closed.
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = SeededDraftTitle })).ToBeVisibleAsync();

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
        await Page.GetByLabel("Body editor")
            .Locator("[contenteditable=true]")
            .FillAsync("Cropped image browser test body.");

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

        var save = Page.Locator("form[data-busy-submit] button[type=submit]");
        // Locator.EvaluateAsync waits for the element to be attached before running, unlike a raw
        // Page.EvaluateAsync — needed here because the redirect from the previous Save can still
        // be resolving when this runs, and a bare document.querySelector would race it.
        await Page.Locator("form.admin-form").EvaluateAsync("""
            (form) => {
              form.addEventListener("submit", event => event.preventDefault(), { once: true });
              form.requestSubmit();
            }
            """);
        await Expect(save).ToHaveTextAsync("Saving…");
        await Expect(save).ToBeDisabledAsync();
        await Page.ReloadAsync();

        await Page.GetByLabel("Article image").SetInputFilesAsync(imagePath);
        await Expect(Page.Locator("[data-article-image-dialog]")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Use this crop" }).ClickAsync();
        await Expect(Page.Locator("[data-article-image-zoom]")).ToBeDisabledAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/admin/news/\\d+/edit"));
        await Expect(Page.GetByText("Article saved.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminCanPanCroppedImageUnderFixedCard()
    {
        await GotoAdminAsync("/admin/news/new");
        var imagePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "design", "crest.jpg"));
        await Page.GetByLabel("Article image").SetInputFilesAsync(imagePath);

        var dialog = Page.Locator("[data-article-image-dialog]");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByText("Drag the photo to position it. Zoom to tighten the crop."))
            .ToBeVisibleAsync();

        await Page.WaitForFunctionAsync("""
            () => {
              const img = document.querySelector("[data-article-image-stage-img]");
              return !!(img && img.cropper && img.cropper.ready);
            }
            """);

        await Page.Locator("[data-article-image-zoom]").EvaluateAsync("""
            (input) => {
              const max = Number(input.max);
              const min = Number(input.min);
              input.value = String(min + (max - min) * 0.6);
              input.dispatchEvent(new Event("input", { bubbles: true }));
            }
            """);

        var afterZoom = await ReadStageCropAsync();
        var stage = Page.Locator("[data-article-image-stage]");
        var box = await stage.BoundingBoxAsync();
        Assert.That(box, Is.Not.Null);
        await Page.Mouse.Move(box!.X + box.Width * 0.55, box.Y + box.Height * 0.55);
        await Page.Mouse.DownAsync();
        await Page.Mouse.Move(box.X + box.Width * 0.2, box.Y + box.Height * 0.2, new() { Steps = 12 });
        await Page.Mouse.UpAsync();

        var after = await ReadStageCropAsync();
        Assert.That(after.Width / (double)after.Height, Is.EqualTo(1.5).Within(0.05));
        Assert.That(
            Math.Abs(after.X - afterZoom.X) + Math.Abs(after.Y - afterZoom.Y),
            Is.GreaterThan(8),
            "Pointer drag should pan the framed crop, not leave a centered default.");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Use this crop" }).ClickAsync();
        Assert.That(await Page.Locator("[data-crop-x]").InputValueAsync(), Is.EqualTo(after.X.ToString()));
        Assert.That(await Page.Locator("[data-crop-y]").InputValueAsync(), Is.EqualTo(after.Y.ToString()));
        Assert.That(await Page.Locator("[data-crop-width]").InputValueAsync(), Is.EqualTo(after.Width.ToString()));
        Assert.That(await Page.Locator("[data-crop-height]").InputValueAsync(), Is.EqualTo(after.Height.ToString()));
    }

    private async Task<(int X, int Y, int Width, int Height)> ReadStageCropAsync()
    {
        var data = await Page.EvaluateAsync<int[]>("""
            () => {
              const img = document.querySelector("[data-article-image-stage-img]");
              const crop = img.cropper.getData(true);
              return [crop.x, crop.y, crop.width, crop.height];
            }
            """);
        return (data[0], data[1], data[2], data[3]);
    }

    private async Task GotoAdminAsync(string path)
    {
        var response = await Page.GotoAsync(path);
        Assert.That(response?.Status, Is.EqualTo(200), $"Expected {path} to load as admin user {AdminEmail}.");
    }
}
