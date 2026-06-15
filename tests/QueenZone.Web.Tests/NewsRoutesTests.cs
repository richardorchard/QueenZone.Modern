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

        Assert.Contains("<link rel=\"canonical\" href=\"/news\">", body);
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
        Assert.Contains("<link rel=\"canonical\" href=\"/news/page/2\">", pageTwo);
        Assert.Contains("<title>QueenZone news – Page 2</title>", pageTwo);
        Assert.Contains("archive-pagination-controls", pageOne);
        Assert.Contains("archive-pagination-prev is-disabled", pageOne);
        Assert.Contains("rel=\"next\" href=\"/news/page/2\"", pageOne);
        Assert.Contains("rel=\"prev\" href=\"/news\"", pageTwo);
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
        Assert.Contains("<link rel=\"canonical\" href=\"/news\">", body);
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
    public async Task NewsDetailRendersByIdAndSlug()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/1003/queenzone-modernisation-begins");

        Assert.Contains("The first local vertical slice", body);
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