using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class ForumTopicPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ForumTopicPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task ForumTopicPageRendersSeedPosts()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/forum/topic/1002/ranking-every-studio-album");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ranking every studio album", body);
        Assert.DoesNotContain("No posts are available in this thread yet.", body);
        Assert.Contains("brightonrock", body);
        Assert.Contains("A Night at the Opera", body);
        Assert.Contains("<strong>26</strong> posts", body);
    }

    [Fact]
    public async Task ForumTopicPageTwoIncludesPagination()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album/page/2");

        Assert.Contains("Page 2 of 2", body);
        Assert.Contains("Archive reply 1125", body);
    }

    [Fact]
    public async Task ForumTopicPageRedirectsPageOneToCanonicalTopicUrl()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/forum/topic/1002/ranking-every-studio-album/page/1");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/forum/topic/1002/ranking-every-studio-album", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ForumTopicPageReturnsNotFoundForMissingTopic()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/forum/topic/9999/missing-thread");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ForumTopicPageRendersAttachmentLink()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");

        Assert.Contains("cdn.queenzone.org/attachments/anoto-setlist-scan.jpg", body);
        Assert.Contains("anoto-setlist-scan.jpg", body);
        Assert.Contains("JPG", body);
        Assert.Contains("278.0 KB", body);
    }
}