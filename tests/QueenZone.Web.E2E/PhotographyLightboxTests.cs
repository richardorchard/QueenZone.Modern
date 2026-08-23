using Microsoft.Playwright;

namespace QueenZone.Web.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class PhotographyLightboxTests : E2EPageTest
{
    [Test]
    public async Task PhotoDetail_SwipeLeftOnPhone_GoesToNextImage()
    {
        var context = await CreateExtraContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            HasTouch = true,
            IsMobile = true,
            ViewportSize = new() { Width = 390, Height = 844 },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("/photography/brian-may/102");

        var pointerEvents = await page.EvaluateAsync<string>(
            """() => getComputedStyle(document.querySelector("[data-photo-lightbox] .qz-lightbox__image")).pointerEvents""");
        Assert.That(pointerEvents, Is.EqualTo("none"));

        var touchAction = await page.EvaluateAsync<string>(
            """() => getComputedStyle(document.querySelector("[data-photo-lightbox] .qz-lightbox__body")).touchAction""");
        Assert.That(touchAction, Is.EqualTo("none"));

        await page.EvaluateAsync("""
            () => {
              const body = document.querySelector("[data-photo-lightbox] .qz-lightbox__body");
              if (!(body instanceof HTMLElement)) {
                throw new Error("lightbox body missing");
              }

              const r = body.getBoundingClientRect();
              const x = r.left + r.width * 0.7;
              const y = r.top + r.height * 0.5;
              const fire = (type, cx) => {
                const touch = new Touch({
                  identifier: 1,
                  target: body,
                  clientX: cx,
                  clientY: y,
                  pageX: cx,
                  pageY: y,
                  screenX: cx,
                  screenY: y
                });
                body.dispatchEvent(new TouchEvent(type, {
                  bubbles: true,
                  cancelable: true,
                  touches: type === "touchend" ? [] : [touch],
                  targetTouches: type === "touchend" ? [] : [touch],
                  changedTouches: [touch]
                }));
              };

              fire("touchstart", x);
              fire("touchmove", x - 80);
              fire("touchend", x - 120);
            }
            """);

        await page.WaitForURLAsync("**/photography/brian-may/103");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Red Special close-up", Level = 1 }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task PhotoDetail_ArrowRight_GoesToNextImage()
    {
        await Page.GotoAsync("/photography/brian-may/102");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForURLAsync("**/photography/brian-may/103");
    }
}
