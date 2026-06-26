using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public NewsRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task HomePageRendersLatestNews()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/");

        Assert.Contains("Latest news", body);
        Assert.Contains("QueenZone modernisation begins", body);
    }

    [Fact]
    public async Task NewsArchiveRendersPublishedNews()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news");

        Assert.Contains("News archive", body);
        Assert.Contains("/news/1003/queenzone-modernisation-begins", body);
    }

    [Fact]
    public async Task NewsArchivePageOneIncludesCanonicalNewsUrl()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news");

        Assert.Contains(TestSiteConfiguration.CanonicalLink("/news"), body);
        Assert.Contains("<title>QueenZone news</title>", body);
        Assert.Contains("Page 1 of 2", body);
        Assert.DoesNotContain("QueenZone news – Page 1", body);
    }

    [Fact]
    public async Task NewsArchivePageTwoRendersNextBatchWithoutRepeatingPageOneItems()
    {
        var client = factory.CreateClient();

        var pageOne = await client.GetStringAsync("/news");
        var pageTwo = await client.GetStringAsync("/news/page/2");

        Assert.Contains("/news/1003/queenzone-modernisation-begins", pageOne);
        Assert.DoesNotContain("/news/1003/queenzone-modernisation-begins", pageTwo);
        Assert.Contains("/news/1005/archive-sample-article-1005", pageTwo);
        Assert.DoesNotContain("/news/1005/archive-sample-article-1005", pageOne);
        Assert.Contains(TestSiteConfiguration.CanonicalLink("/news/page/2"), pageTwo);
        Assert.Contains("<title>QueenZone news &#x2013; Page 2</title>", pageTwo);
        Assert.Contains("archive-pagination-controls", pageOne);
        Assert.Contains("archive-pagination-prev is-disabled", pageOne);
        Assert.Contains(TestSiteConfiguration.NextLink("/news/page/2"), pageOne);
        Assert.Contains(TestSiteConfiguration.PrevLink("/news"), pageTwo);
        Assert.Contains("archive-pagination-next is-disabled", pageTwo);
    }

    [Fact]
    public async Task NewsArchivePageOneRedirectsFromPagedRoute()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/news/page/1");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/news", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OutOfRangeArchivePageReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/news/page/99");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EmptyArchiveShowsMessageAndRejectsLaterPages()
    {
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new InMemoryNewsRepository([]));
            })).CreateClient();

        var body = await client.GetStringAsync("/news");
        var response = await client.GetAsync("/news/page/2");

        Assert.Contains("No published news is available yet.", body);
        Assert.Contains(TestSiteConfiguration.CanonicalLink("/news"), body);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HiddenNewsRecordsAreExcludedFromArchive()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news");

        Assert.DoesNotContain("Hidden moderation draft", body);
        Assert.DoesNotContain("/news/9001/", body);
    }

    [Fact]
    public async Task NewsDetailRendersCompletePublishedArticle()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/1003/queenzone-modernisation-begins");

        Assert.Contains("The first local vertical slice", body);
        Assert.Contains("Back to news archive", body);
        Assert.Contains("<time datetime=\"2026-06-11\">", body);
        Assert.Contains(TestSiteConfiguration.CanonicalLink("/news/1003/queenzone-modernisation-begins"), body);
        Assert.Contains("<meta name=\"description\" content=\"The first local vertical slice", body);
        Assert.Contains("<title>QueenZone modernisation begins | QueenZone news</title>", body);
    }

    [Fact]
    public async Task MissingNewsArticleReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/news/999999/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HiddenNewsArticleReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/news/9001/hidden-moderation-draft");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NewsDetailRendersSafeSourceLinkAndRejectsUnsafeUrls()
    {
        var items = new[]
        {
            new NewsItem(
                5001,
                "Article with source",
                "Excerpt with source.",
                "Published body.",
                new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc),
                "https://example.com/original-story",
                true),
            new NewsItem(
                5002,
                "Article with unsafe source",
                "Unsafe source excerpt.",
                "Published body.",
                new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc),
                "javascript:alert(1)",
                true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new InMemoryNewsRepository(items));
            })).CreateClient();

        var safeBody = await client.GetStringAsync("/news/5001/article-with-source");
        var unsafeBody = await client.GetStringAsync("/news/5002/article-with-unsafe-source");

        Assert.Contains("href=\"https://example.com/original-story\"", safeBody);
        Assert.Contains("rel=\"noopener noreferrer\">Source</a>", safeBody);
        Assert.DoesNotContain("javascript:", unsafeBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Source</a>", unsafeBody);
    }

    [Fact]
    public async Task NewsDetailSanitizesUnsafeLegacyHtmlInBody()
    {
        var items = new[]
        {
            new NewsItem(
                5003,
                "Unsafe HTML article",
                "Unsafe excerpt.",
                "<script>alert('xss')</script><p>Safe <strong>legacy</strong> paragraph</p>",
                new DateTime(2026, 5, 3, 9, 0, 0, DateTimeKind.Utc),
                null,
                true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new InMemoryNewsRepository(items));
            })).CreateClient();

        var body = await client.GetStringAsync("/news/5003/unsafe-html-article");

        Assert.DoesNotContain("alert", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Safe <strong>legacy</strong> paragraph</p>", body);
    }

    [Fact]
    public async Task DuplicateLegacyRowsResolveToLatestPublishedDetailWithoutError()
    {
        var items = new[]
        {
            new NewsItem(
                4242,
                "Latest duplicate title",
                "Latest excerpt",
                "<p>Latest duplicate body</p>",
                new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc),
                null,
                true),
            new NewsItem(
                4242,
                "Older duplicate title",
                "Older excerpt",
                "<p>Older duplicate body</p>",
                new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                null,
                true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new InMemoryNewsRepository(items));
            })).CreateClient();

        var body = await client.GetStringAsync("/news/4242/latest-duplicate-title");

        Assert.Contains("Latest duplicate body", body);
        Assert.DoesNotContain("Older duplicate body", body);
        Assert.DoesNotContain("Older duplicate title", body);
    }

    [Fact]
    public async Task WrongNewsSlugRedirectsToCanonicalSlug()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/news/1003/not-the-right-slug");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/news/1003/queenzone-modernisation-begins", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OldNewsUrlsAreNotSpecialCased()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/process/news_view.aspx?news_id=1003");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NewsArchiveOrdersByCreatedDateDescending()
    {
        var items = new[]
        {
            new NewsItem(
                3001,
                "Oldest article",
                "Oldest excerpt.",
                "Oldest body.",
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                true),
            new NewsItem(
                3002,
                "Newest article",
                "Newest excerpt.",
                "Newest body.",
                new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                true),
            new NewsItem(
                3003,
                "Middle article",
                "Middle excerpt.",
                "Middle body.",
                new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                null,
                true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new InMemoryNewsRepository(items));
            })).CreateClient();

        var body = await client.GetStringAsync("/news");
        var dates = Regex.Matches(body, "<time datetime=\"(\\d{4}-\\d{2}-\\d{2})\">")
            .Select(match => DateOnly.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Take(3)
            .ToList();

        Assert.Equal(
            new[] { new DateOnly(2024, 6, 1), new DateOnly(2022, 3, 15), new DateOnly(2020, 1, 1) },
            dates);
        Assert.Contains("Newest article", body);
        Assert.Contains("Middle article", body);
        Assert.Contains("Oldest article", body);
    }

    [Fact]
    public async Task DuplicateLegacyRowsAreDeduplicatedBeforePaging()
    {
        var duplicateItems = Enumerable.Range(1, 25)
            .Select(id => new NewsItem(
                id,
                $"Published article {id}",
                $"Excerpt {id}",
                $"Body {id}",
                new DateTime(2026, 1, id, 0, 0, 0, DateTimeKind.Utc),
                null,
                true))
            .ToList();

        duplicateItems.Add(new NewsItem(
            5,
            "Duplicate copy of article 5",
            "Older duplicate excerpt",
            "Older duplicate body",
            new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            null,
            true));

        duplicateItems.Add(new NewsItem(
            99,
            "Hidden duplicate candidate",
            "Should not render",
            "Should not render",
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            null,
            false));

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new InMemoryNewsRepository(duplicateItems));
            })).CreateClient();

        var pageOne = await client.GetStringAsync("/news");
        var pageTwo = await client.GetStringAsync("/news/page/2");

        Assert.Contains("Published article 25", pageOne);
        Assert.DoesNotContain("Published article 5", pageOne);
        Assert.Contains("Published article 5", pageTwo);
        Assert.DoesNotContain("Duplicate copy of article 5", pageOne);
        Assert.DoesNotContain("Duplicate copy of article 5", pageTwo);
        Assert.DoesNotContain("Hidden duplicate candidate", pageOne);
        Assert.DoesNotContain("Hidden duplicate candidate", pageTwo);
    }
}
