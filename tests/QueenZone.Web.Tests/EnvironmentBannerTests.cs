using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class EnvironmentBannerTests
{
    [Theory]
    [InlineData("www.queenzone.org")]
    [InlineData("WWW.QUEENZONE.ORG")]
    [InlineData("www.queenzone.org.")]
    [InlineData("queenzone.org")]
    [InlineData("QUEENZONE.ORG")]
    [InlineData("queenzone.org.")]
    public void ResolveLabel_hides_on_production_hosts(string host)
    {
        Assert.Null(EnvironmentBanner.ResolveLabel(host));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("localhost.")]
    [InlineData("app.localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    public void ResolveLabel_uses_local_copy_for_loopback(string host)
    {
        Assert.Equal(EnvironmentBanner.LocalLabel, EnvironmentBanner.ResolveLabel(host));
    }

    [Theory]
    [InlineData("dev.queenzone.org")]
    [InlineData("DEV.QUEENZONE.ORG")]
    [InlineData("dev.queenzone.org.")]
    public void ResolveLabel_uses_dev_copy_for_dev_host(string host)
    {
        Assert.Equal(EnvironmentBanner.DevLabel, EnvironmentBanner.ResolveLabel(host));
    }

    [Theory]
    [InlineData("queenzone-dev.azurewebsites.net")]
    [InlineData("queenzone-devbox.azurewebsites.net")]
    [InlineData("preview.queenzone.test")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveLabel_uses_non_prod_copy_for_other_hosts(string? host)
    {
        Assert.Equal(EnvironmentBanner.NonProdLabel, EnvironmentBanner.ResolveLabel(host));
    }

    [Fact]
    public void ResolveLabel_never_returns_azure_resource_name()
    {
        Assert.DoesNotContain(
            "queenzone-dev",
            EnvironmentBanner.ResolveLabel("queenzone-dev.azurewebsites.net"),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "queenzone-dev",
            EnvironmentBanner.ResolveLabel("dev.queenzone.org"),
            StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class EnvironmentBannerIntegrationTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public EnvironmentBannerIntegrationTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("localhost", "LOCAL")]
    [InlineData("127.0.0.1", "LOCAL")]
    [InlineData("dev.queenzone.org", "DEV")]
    [InlineData("queenzone-dev.azurewebsites.net", "NON-PROD")]
    [InlineData("preview.queenzone.test", "NON-PROD")]
    public async Task PublicPage_renders_reserved_banner_for_non_prod_host(string host, string label)
    {
        var html = await GetHtmlAsync(host);

        Assert.Contains("class=\"qz-env-banner\"", html, StringComparison.Ordinal);
        Assert.Contains($"data-testid=\"env-banner\">{label}</div>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("queenzone-dev", html, StringComparison.Ordinal);
        var bannerStart = html.IndexOf("class=\"qz-env-banner\"", StringComparison.Ordinal);
        var bannerEnd = html.IndexOf("</div>", bannerStart, StringComparison.Ordinal);
        Assert.True(bannerStart >= 0 && bannerEnd > bannerStart);
        var bannerHtml = html[bannerStart..bannerEnd];
        Assert.DoesNotContain("<button", bannerHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("<a ", bannerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicPage_hides_banner_on_www_production_host()
    {
        var html = await GetHtmlAsync("www.queenzone.org");

        Assert.DoesNotContain("qz-env-banner", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"env-banner\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">LOCAL<", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">DEV<", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">NON-PROD<", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Banner_sits_between_skip_link_and_header()
    {
        var html = await GetHtmlAsync("localhost");
        var skipIndex = html.IndexOf("Skip to content", StringComparison.Ordinal);
        var bannerIndex = html.IndexOf("qz-env-banner", StringComparison.Ordinal);
        var headerIndex = html.IndexOf("<header", StringComparison.OrdinalIgnoreCase);

        Assert.True(skipIndex >= 0 && bannerIndex > skipIndex && bannerIndex < headerIndex);
    }

    private async Task<string> GetHtmlAsync(string host)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = host;

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
