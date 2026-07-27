using Microsoft.AspNetCore.Http;

namespace QueenZone.Web.Tests;

public sealed class CanonicalHostRedirectTests
{
    [Fact]
    public void TryBuildRedirectUrl_redirects_apex_host_to_configured_www_host()
    {
        var request = CreateRequest("queenzone.org", "/news", "?utm_source=search-console");

        var shouldRedirect = CanonicalHostRedirect.TryBuildRedirectUrl(
            request,
            "https://www.queenzone.org",
            out var redirectUrl);

        Assert.True(shouldRedirect);
        Assert.Equal("https://www.queenzone.org/news?utm_source=search-console", redirectUrl);
    }

    [Fact]
    public void TryBuildRedirectUrl_does_not_redirect_canonical_host()
    {
        var request = CreateRequest("www.queenzone.org", "/news", string.Empty);

        var shouldRedirect = CanonicalHostRedirect.TryBuildRedirectUrl(
            request,
            "https://www.queenzone.org",
            out var redirectUrl);

        Assert.False(shouldRedirect);
        Assert.Empty(redirectUrl);
    }

    [Fact]
    public void TryBuildRedirectUrl_does_not_redirect_when_public_host_is_not_www()
    {
        var request = CreateRequest("queenzone.test", "/", string.Empty);

        var shouldRedirect = CanonicalHostRedirect.TryBuildRedirectUrl(
            request,
            "https://preview.queenzone.test",
            out var redirectUrl);

        Assert.False(shouldRedirect);
        Assert.Empty(redirectUrl);
    }

    private static HttpRequest CreateRequest(string host, string path, string queryString)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        context.Request.QueryString = QueryString.FromUriComponent(queryString);
        return context.Request;
    }
}
