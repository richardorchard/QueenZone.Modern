using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace QueenZone.Web.E2E;

/// <summary>
/// Public visitor browser smoke: homepage, news, forum, and related archive surfaces.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SmokeTests : E2EPageTest
{
    [Test]
    public async Task Homepage_ShowsLatestNews()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByText("Latest news")).ToBeVisibleAsync();
        await Expect(Page.Locator("a.qz-card[href='/news']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task NewsArchive_ShowsArchiveHeading()
    {
        await Page.GotoAsync("/news");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "News", Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task NewsArchive_Pagination_NavigatesToNextPage()
    {
        await Page.GotoAsync("/news");

        var pageOneSummary = await Page.Locator(".archive-pagination-summary").First.InnerTextAsync();
        Assert.That(pageOneSummary, Does.Contain("Page 1"));

        var firstTitle = await Page.Locator(".qz-news-row a").First.InnerTextAsync();

        await Page.Locator("a.archive-pagination-next").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/news/page/2/?$"));

        var pageTwoSummary = await Page.Locator(".archive-pagination-summary").First.InnerTextAsync();
        Assert.That(pageTwoSummary, Does.Contain("Page 2"));

        var secondTitle = await Page.Locator(".qz-news-row a").First.InnerTextAsync();
        Assert.That(secondTitle, Is.Not.EqualTo(firstTitle));
    }

    [Test]
    public async Task NewsDetail_ShowsCanonicalAndBody()
    {
        await Page.GotoAsync("/news/1003/queenzone-modernisation-begins");

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = "QueenZone modernisation begins",
            Level = 1
        })).ToBeVisibleAsync();

        await Expect(Page.Locator("article.article-body")).ToBeVisibleAsync();
        await Expect(Page.Locator("article.article-body")).ToContainTextAsync("placeholder item");

        var canonical = Page.Locator("link[rel='canonical']");
        await Expect(canonical).ToHaveCountAsync(1);
        var href = await canonical.GetAttributeAsync("href");
        Assert.That(href, Does.Contain("/news/1003/queenzone-modernisation-begins"));
    }

    [Test]
    public async Task ForumIndex_ShowsBoards()
    {
        await Page.GotoAsync("/forum");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Forum", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "The Music" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ForumCategory_ListsTopics()
    {
        await Page.GotoAsync("/forum/1/the-music");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "The Music", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Ranking every studio album" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ForumTopic_ShowsPostsAndBreadcrumbs()
    {
        await Page.GotoAsync("/forum/topic/1002/ranking-every-studio-album");

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Ranking every studio album",
            Level = 1
        })).ToBeVisibleAsync();

        var breadcrumbs = Page.GetByRole(AriaRole.Navigation, new() { Name = "Breadcrumb" });
        await Expect(breadcrumbs).ToBeVisibleAsync();
        await Expect(breadcrumbs.GetByRole(AriaRole.Link, new() { Name = "Forum", Exact = true })).ToBeVisibleAsync();

        // Sample seed posts include discussion content for this topic.
        await Expect(Page.Locator(".qz-forum-posts")).ToBeVisibleAsync();
        await Expect(Page.Locator(".qz-forum-post").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task ArticlesArchive_ShowsHeading()
    {
        await Page.GotoAsync("/articles");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Articles", Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task BiographyIndex_ShowsHeading()
    {
        await Page.GotoAsync("/biography");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Biography", Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task PhotographyIndex_ShowsHeading()
    {
        await Page.GotoAsync("/photography");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Photography", Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_ShowsSearchForm()
    {
        await Page.GotoAsync("/search");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Search", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.Locator("#qz-search")).ToBeVisibleAsync();
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

        var noHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1");
        Assert.That(noHorizontalOverflow, Is.True, "Homepage should not require horizontal scrolling on a phone viewport.");
    }

    [Test]
    public async Task MobileViewport_OpensNavigationMenu()
    {
        await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync("/");
        await page.Locator("[data-menu-open]").ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Primary navigation" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "News", Exact = true })).ToBeVisibleAsync();
    }
}
