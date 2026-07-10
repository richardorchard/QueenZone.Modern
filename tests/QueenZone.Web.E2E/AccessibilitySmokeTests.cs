using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace QueenZone.Web.E2E;

/// <summary>
/// axe-core smoke: critical accessibility issues fail the build.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class AccessibilitySmokeTests : E2EPageTest
{
    [Test]
    public async Task Homepage_HasNoCriticalAxeViolations()
    {
        await Page.GotoAsync("/");
        await Expect(Page.GetByText("Latest news")).ToBeVisibleAsync();

        await AssertNoCriticalViolationsAsync(Page);
    }

    [Test]
    public async Task NewsDetail_HasNoCriticalAxeViolations()
    {
        await Page.GotoAsync("/news/1003/queenzone-modernisation-begins");
        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = "QueenZone modernisation begins",
            Level = 1
        })).ToBeVisibleAsync();

        await AssertNoCriticalViolationsAsync(Page);
    }

    [Test]
    public async Task ForumTopic_HasNoCriticalAxeViolations()
    {
        await Page.GotoAsync("/forum/topic/1002/ranking-every-studio-album");
        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Ranking every studio album",
            Level = 1
        })).ToBeVisibleAsync();

        await AssertNoCriticalViolationsAsync(Page);
    }

    private static async Task AssertNoCriticalViolationsAsync(Microsoft.Playwright.IPage page)
    {
        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions
            {
                Type = "tag",
                Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]
            }
        };

        AxeResult results = await page.RunAxe(options);
        var critical = results.Violations
            .Where(v => string.Equals(v.Impact, "critical", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var serious = results.Violations
            .Where(v => string.Equals(v.Impact, "serious", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (serious.Count > 0)
        {
            TestContext.Out.WriteLine(
                "Serious (non-blocking) axe findings:" + Environment.NewLine + FormatViolations(serious));
        }

        Assert.That(
            critical,
            Is.Empty,
            "Critical axe-core violations:" + Environment.NewLine + FormatViolations(critical));
    }

    private static string FormatViolations(IEnumerable<AxeResultItem> items) =>
        string.Join(
            Environment.NewLine,
            items.Select(v =>
            {
                var nodeCount = v.Nodes?.Count() ?? 0;
                return $"- [{v.Impact}] {v.Id}: {v.Help} ({v.HelpUrl}) nodes={nodeCount}";
            }));
}
