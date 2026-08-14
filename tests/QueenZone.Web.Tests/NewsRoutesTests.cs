using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public NewsRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
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
        TestHtmlAssertions.AssertPageTitle(body, "QueenZone news");
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
        TestHtmlAssertions.AssertPageTitle(pageTwo, "QueenZone news – Page 2");
        Assert.Contains(
            "meta name=\"description\" content=\"The latest Queen news and stories from QueenZone - page 2.\"",
            pageTwo);
        Assert.Contains(
            "meta name=\"description\" content=\"The latest Queen news and stories from QueenZone.\"",
            pageOne);
        Assert.Contains(TestSiteConfiguration.NextLink("/news/page/2"), pageOne);
        Assert.DoesNotContain(TestSiteConfiguration.PrevLink("/news"), pageOne);
        Assert.Contains(TestSiteConfiguration.PrevLink("/news"), pageTwo);
        Assert.DoesNotContain(TestSiteConfiguration.NextLink("/news/page/3"), pageTwo);
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
                services.AddSingleton<INewsRepository>(new FixedNewsRepository([]));
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
        Assert.Contains("qz-breadcrumbs", body);
        Assert.Contains(">News<", body);
        Assert.Contains("aria-current=\"page\">QueenZone modernisation begins</span>", body);
        Assert.Contains("\"@type\":\"BreadcrumbList\"", body);
        Assert.Contains("<time datetime=\"2026-06-11\">", body);
        Assert.Contains(TestSiteConfiguration.CanonicalLink("/news/1003/queenzone-modernisation-begins"), body);
        Assert.Contains("<meta name=\"description\" content=\"The first local vertical slice", body);
        TestHtmlAssertions.AssertPageTitle(body, "QueenZone modernisation begins | QueenZone news");
    }

    [Fact]
    public async Task NewsArchiveAndDetail_LinkVerifiedSubmitterProfile()
    {
        var submitterMemberId = Guid.NewGuid();
        var item = new NewsItem(
            5100,
            "Member submitted news",
            "Member-submitted excerpt.",
            "Member-submitted body.",
            new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc),
            null,
            true,
            SubmitterMemberId: submitterMemberId,
            SubmitterDisplayName: "News Contributor");
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INewsRepository>(new FixedNewsRepository([item]));
            })).CreateClient();

        var archiveBody = await client.GetStringAsync("/news");
        var detailBody = await client.GetStringAsync("/news/5100/member-submitted-news");

        Assert.Contains($"Submitted by <a class=\"qz-attribution-link\" href=\"/members/{submitterMemberId}\">News Contributor</a>", archiveBody);
        Assert.Contains($"Submitted by <a class=\"qz-attribution-link\" href=\"/members/{submitterMemberId}\">News Contributor</a>", detailBody);
    }

    [Fact]
    public async Task LegacyNews_DoesNotShowUnverifiedSubmitterAttribution()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/1003/queenzone-modernisation-begins");

        Assert.DoesNotContain("Submitted by", body);
    }

    [Fact]
    public async Task InMemoryNewsRepository_AddsPromotedSuggestionAttribution()
    {
        var memberId = Guid.NewGuid();
        var member = new QueenZone.Data.Entities.MemberAccount
        {
            Id = memberId,
            Email = "in-memory-news@example.com",
            DisplayName = "In-memory Contributor",
        };
        var suggestions = new InMemoryNewsSuggestionRepository(id => id == memberId ? member : null);
        var suggestion = await suggestions.CreateAsync(new NewsSuggestion(
            Guid.NewGuid(),
            memberId,
            "https://example.com/in-memory-news",
            NewsCandidateDedupe.ComputeUrlHash("https://example.com/in-memory-news"),
            "In-memory news",
            null,
            NewsSuggestionStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null));
        await suggestions.PromoteAsync(suggestion.Id, 5200, "admin@test.local", null);
        var publishedAt = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                5200,
                "In-memory news",
                "in-memory-news",
                "Excerpt",
                "Body",
                publishedAt,
                null,
                true,
                publishedAt,
                publishedAt,
                "admin@test.local"),
        ]);

        var item = await new InMemoryNewsRepository(store, suggestions).GetByIdAsync(5200);

        Assert.Equal(memberId, item!.SubmitterMemberId);
        Assert.Equal("In-memory Contributor", item.SubmitterDisplayName);
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
                services.AddSingleton<INewsRepository>(new FixedNewsRepository(items));
            })).CreateClient();

        var safeBody = await client.GetStringAsync("/news/5001/article-with-source");
        var unsafeBody = await client.GetStringAsync("/news/5002/article-with-unsafe-source");

        Assert.Contains("href=\"https://example.com/original-story\"", safeBody);
        Assert.Contains("rel=\"noopener noreferrer\"", safeBody);
        Assert.Contains(">https://example.com/original-story</a>", safeBody);
        Assert.Contains("Source: <a", safeBody);
        Assert.DoesNotContain("javascript:", unsafeBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("class=\"article-source\"", unsafeBody);
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
                services.AddSingleton<INewsRepository>(new FixedNewsRepository(items));
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
                services.AddSingleton<INewsRepository>(new FixedNewsRepository(items));
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
                services.AddSingleton<INewsRepository>(new FixedNewsRepository(items));
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
                services.AddSingleton<INewsRepository>(new FixedNewsRepository(duplicateItems));
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
