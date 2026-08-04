using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class SearchPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SearchPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task SearchPageRendersWithoutQuery()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search");

        Assert.Contains("Search the Archive", body);
        Assert.Contains("Bohemian Rhapsody", body);
        Assert.Contains("Freddie Mercury", body);
        Assert.DoesNotContain("result", body);
    }

    [Fact]
    public async Task SearchPageRendersForumResultsForMatchingQuery()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search?q=studio+album");

        Assert.Contains("studio album", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ranking every studio album", body);
        Assert.Contains("/forum/topic/1002/ranking-every-studio-album", body);
    }

    [Fact]
    public async Task SearchPageRendersNewsResultsForMatchingQuery()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search?q=modernisation");

        Assert.Contains("QueenZone modernisation begins", body);
        Assert.Contains("/news/1003/", body);
    }

    [Fact]
    public async Task SearchPageRendersNoResultsMessageForUnmatchedQuery()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search?q=xyzzy_no_match_zzzqq");

        Assert.Contains("No results found", body);
    }

    [Fact]
    public async Task SearchPageTypeFilterNarrowsToOneContentType()
    {
        var client = factory.CreateClient();

        // "archive" matches both forum threads and news articles in seed data.
        var allBody = await client.GetStringAsync("/search?q=archive");
        var newsOnlyBody = await client.GetStringAsync("/search?q=archive&type=news");

        Assert.Contains("Forum", allBody);
        Assert.DoesNotContain("/forum/topic/", newsOnlyBody);
    }

    [Fact]
    public async Task SearchPageTypeFilterTabsLinkWithTypeParameter()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search?q=archive");

        Assert.Contains("href=\"/search?q=archive&amp;type=news\"", body);
        Assert.Contains("href=\"/search?q=archive&amp;type=forum\"", body);
    }

    [Fact]
    public async Task SearchPageInvalidTypeFilterFallsBackToAllResults()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search?q=archive&type=not-a-real-type");

        Assert.DoesNotContain("No results found", body);
    }

    [Fact]
    public async Task SearchPageShowsPaginationWhenResultsExceedPageSize()
    {
        var client = factory.CreateClient();

        // "archive" matches many seed threads (Archive sample thread 1004–1030)
        var body = await client.GetStringAsync("/search?q=archive&page=1");

        Assert.Contains("Page 1 of", body);
        Assert.Contains("Next", body);
    }

    [Fact]
    public async Task SearchPageExampleTagsLinkToSearch()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search");

        Assert.Contains("href=\"/search?q=Bohemian%20Rhapsody\"", body);
        Assert.Contains("href=\"/search?q=Freddie%20Mercury\"", body);
    }

    [Fact]
    public async Task SearchPageEmitsNoindexFollowRobotsTag()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search");

        Assert.Contains("""<meta name="robots" content="noindex,follow">""", body);
    }

    [Fact]
    public async Task SearchPageWithQueryEmitsNoindexFollowRobotsTag()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/search?q=studio+album");

        Assert.Contains("""<meta name="robots" content="noindex,follow">""", body);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/forum")]
    [InlineData("/news")]
    [InlineData("/articles")]
    public async Task IndexablePagesDoNotEmitNoindexRobotsTag(string path)
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync(path);

        Assert.DoesNotContain("""<meta name="robots" content="noindex""", body);
    }
}
