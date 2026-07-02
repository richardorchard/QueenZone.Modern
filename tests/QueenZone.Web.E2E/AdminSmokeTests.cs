using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace QueenZone.Web.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class AdminSmokeTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://127.0.0.1:5099";

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
    public async Task AdminNews_ShowsEditorialList()
    {
        await Page.GotoAsync("/admin/news");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin news", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Create article" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminNews_CanCreateDraft()
    {
        var uniqueTitle = $"E2E admin draft {DateTime.UtcNow:yyyyMMddHHmmss}";

        await Page.GotoAsync("/admin/news/new");
        await Page.GetByLabel("Title").FillAsync(uniqueTitle);
        await Page.GetByLabel("Excerpt").FillAsync("Playwright admin smoke excerpt.");
        await Page.GetByLabel("Body").FillAsync("Playwright admin smoke body.");
        await Page.GetByLabel("Publication date").FillAsync("2026-06-14");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/admin/news/\\d+/edit"));
        await Expect(Page.GetByText(uniqueTitle)).ToBeVisibleAsync();
    }
}
