using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace QueenZone.Web.E2E;

/// <summary>
/// Shared Playwright page fixture: base URL, tracing, and failure screenshots/traces.
/// </summary>
public abstract class E2EPageTest : PageTest
{
    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://127.0.0.1:5099";

    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            BaseURL = BaseUrl,
        };

    [SetUp]
    public async Task StartTracingAsync()
    {
        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Title = TestContext.CurrentContext.Test.FullName,
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    [TearDown]
    public async Task CaptureFailureArtifactsAsync()
    {
        var outcome = TestContext.CurrentContext.Result.Outcome.Status;
        var failed = outcome is TestStatus.Failed or TestStatus.Warning;

        if (!failed)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var dir = E2EArtifactPaths.EnsureDirectory();
        var name = E2EArtifactPaths.SanitizeFileName(TestContext.CurrentContext.Test.Name);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        try
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(dir, $"{name}-{stamp}.png"),
                FullPage = true
            });
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Screenshot capture failed: {ex.Message}");
        }

        try
        {
            await Context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(dir, $"{name}-{stamp}.zip")
            });
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Trace capture failed: {ex.Message}");
            try
            {
                await Context.Tracing.StopAsync();
            }
            catch
            {
                // ignore
            }
        }
    }
}
