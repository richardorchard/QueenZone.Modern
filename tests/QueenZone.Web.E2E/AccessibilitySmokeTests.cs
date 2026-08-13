using Microsoft.Playwright;

namespace QueenZone.Web.E2E;

/// <summary>
/// axe-core smoke: critical accessibility issues fail the build.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class AccessibilitySmokeTests : E2EPageTest
{
    [Test]
    public async Task Homepage_SkipLink_MovesFocusToMainContent()
    {
        await Page.GotoAsync("/");
        await Expect(Page.GetByText("Latest news")).ToBeVisibleAsync();

        var skipLink = Page.GetByRole(AriaRole.Link, new() { Name = "Skip to content" });
        await Expect(skipLink).ToBeAttachedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(skipLink).ToBeFocusedAsync();

        await skipLink.PressAsync("Enter");

        var main = Page.GetByRole(AriaRole.Main);
        await Expect(main).ToHaveAttributeAsync("id", "main-content");
        await Expect(main).ToBeFocusedAsync();
    }

    [Test]
    public async Task Homepage_HasNoCriticalAxeViolations()
    {
        await Page.GotoAsync("/");
        await Expect(Page.GetByText("Latest news")).ToBeVisibleAsync();

        await AxeAssertions.AssertNoCriticalViolationsAsync(Page);
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

        await AxeAssertions.AssertNoCriticalViolationsAsync(Page);
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

        await AxeAssertions.AssertNoCriticalViolationsAsync(Page);
    }
}
