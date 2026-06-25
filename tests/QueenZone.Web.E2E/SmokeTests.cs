using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace QueenZone.Web.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SmokeTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://127.0.0.1:5099";

    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            BaseURL = BaseUrl,
        };

    [Test]
    public async Task Homepage_ShowsLatestNews()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByText("Latest news")).ToBeVisibleAsync();
    }

    [Test]
    public async Task NewsArchive_ShowsArchiveHeading()
    {
        await Page.GotoAsync("/news");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "News", Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Homepage_RendersOnMobileViewport()
    {
        await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync("/");

        await Expect(page.GetByText("Latest news")).ToBeVisibleAsync();
    }
}
