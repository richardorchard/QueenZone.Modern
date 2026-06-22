using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class StoriesRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public StoriesRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task StoriesArchiveRendersPublishedStories()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/stories");

        Assert.Contains("Stories", body);
        Assert.Contains("/stories/101/inside-the-making-of-bohemian-rhapsody", body);
    }

    [Fact]
    public async Task StoriesArchivePageOneIncludesCanonicalStoriesUrl()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/stories");

        Assert.Contains("<link rel=\"canonical\" href=\"/stories\">", body);
        Assert.Contains("<title>QueenZone stories</title>", body);
        Assert.Contains("Page 1 of 2", body);
    }

    [Fact]
    public async Task StoriesArchivePageTwoRendersNextBatchWithoutRepeatingPageOneItems()
    {
        var client = factory.CreateClient();

        var pageOne = await client.GetStringAsync("/stories");
        var pageTwo = await client.GetStringAsync("/stories/page/2");

        Assert.Contains("/stories/101/inside-the-making-of-bohemian-rhapsody", pageOne);
        Assert.DoesNotContain("/stories/101/inside-the-making-of-bohemian-rhapsody", pageTwo);
        Assert.Contains("/stories/121/archive-sample-story-121", pageTwo);
        Assert.Contains("<link rel=\"canonical\" href=\"/stories/page/2\">", pageTwo);
        Assert.Contains("rel=\"prev\" href=\"/stories\"", pageTwo);
    }

    [Fact]
    public async Task StoriesArchivePageOneRedirectsFromPagedRoute()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/stories/page/1");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/stories", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OutOfRangeArchivePageReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/stories/page/99");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EmptyArchiveShowsMessageAndRejectsLaterPages()
    {
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStoriesRepository>(new InMemoryStoriesRepository([]));
            })).CreateClient();

        var body = await client.GetStringAsync("/stories");
        var response = await client.GetAsync("/stories/page/2");

        Assert.Contains("No published stories are available yet.", body);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HiddenStoryRecordsAreExcludedFromArchive()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/stories");

        Assert.DoesNotContain("Hidden moderation draft", body);
        Assert.DoesNotContain("/stories/9001/", body);
    }

    [Fact]
    public async Task StoryDetailRendersCompletePublishedArticle()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/stories/101/inside-the-making-of-bohemian-rhapsody");

        Assert.Contains("Six weeks, three studios", body);
        Assert.Contains("Back to stories archive", body);
        Assert.Contains("Recording", body);
        Assert.Contains("<link rel=\"canonical\" href=\"/stories/101/inside-the-making-of-bohemian-rhapsody\">", body);
        Assert.Contains("<title>Inside the Making of Bohemian Rhapsody | QueenZone stories</title>", body);
    }

    [Fact]
    public async Task MissingStoryReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/stories/999999/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HiddenStoryReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/stories/9001/hidden-moderation-draft");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StoryDetailRendersSafeSourceLinkAndPlainTextAttribution()
    {
        var items = new[]
        {
            new StoryItem(
                5001,
                "Article with source link",
                "Excerpt with source.",
                "<p>Published body.</p>",
                new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc),
                "https://example.com/original-story",
                "Features",
                true),
            new StoryItem(
                5002,
                "Article with attribution",
                "Attribution excerpt.",
                "<p>Published body.</p>",
                new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc),
                "Queen Magazine",
                "Features",
                true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStoriesRepository>(new InMemoryStoriesRepository(items));
            })).CreateClient();

        var linkedBody = await client.GetStringAsync("/stories/5001/article-with-source-link");
        var attributedBody = await client.GetStringAsync("/stories/5002/article-with-attribution");

        Assert.Contains("href=\"https://example.com/original-story\"", linkedBody);
        Assert.Contains("Queen Magazine", attributedBody);
        Assert.DoesNotContain("href=\"Queen Magazine\"", attributedBody);
    }

    [Fact]
    public async Task StoryDetailSanitizesUnsafeLegacyHtmlInBody()
    {
        var items = new[]
        {
            new StoryItem(
                5003,
                "Unsafe HTML story",
                "Unsafe excerpt.",
                "<script>alert('xss')</script><p>Safe <strong>legacy</strong> paragraph</p>",
                new DateTime(2026, 5, 3, 9, 0, 0, DateTimeKind.Utc),
                null,
                null,
                true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStoriesRepository>(new InMemoryStoriesRepository(items));
            })).CreateClient();

        var body = await client.GetStringAsync("/stories/5003/unsafe-html-story");

        Assert.DoesNotContain("alert", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Safe <strong>legacy</strong> paragraph</p>", body);
    }

    [Fact]
    public async Task WrongStorySlugRedirectsToCanonicalSlug()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/stories/101/not-the-right-slug");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/stories/101/inside-the-making-of-bohemian-rhapsody", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OldArticleUrlsAreNotSpecialCased()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/process/article_show.aspx?q=101");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StoriesArchiveOrdersByCreatedDateDescending()
    {
        var items = new[]
        {
            new StoryItem(3001, "Oldest story", "Oldest excerpt.", "<p>Oldest body.</p>", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, null, true),
            new StoryItem(3002, "Newest story", "Newest excerpt.", "<p>Newest body.</p>", new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), null, null, true),
            new StoryItem(3003, "Middle story", "Middle excerpt.", "<p>Middle body.</p>", new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc), null, null, true)
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStoriesRepository>(new InMemoryStoriesRepository(items));
            })).CreateClient();

        var body = await client.GetStringAsync("/stories");
        var dates = Regex.Matches(body, "<time datetime=\"(\\d{4}-\\d{2}-\\d{2})\">")
            .Select(match => DateOnly.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Take(3)
            .ToList();

        Assert.Equal(
            new[] { new DateOnly(2024, 6, 1), new DateOnly(2022, 3, 15), new DateOnly(2020, 1, 1) },
            dates);
    }
}