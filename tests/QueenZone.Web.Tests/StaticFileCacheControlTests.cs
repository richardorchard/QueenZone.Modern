using Microsoft.AspNetCore.Http;

namespace QueenZone.Web.Tests;

public sealed class StaticFileCacheControlTests
{
    [Theory]
    [InlineData("/css/site.css?v=abc123", true)]
    [InlineData("/js/site.js?v=abc123", true)]
    [InlineData("/js/home-archive-hero.js?v=abc123", true)]
    [InlineData("/css/site.css", false)]
    [InlineData("/design-system/assets/crest-white.png", false)]
    public void IsVersionedRequest_detects_asp_append_version_query(string path, bool expected)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(path.Contains('?')
            ? path[path.IndexOf('?', StringComparison.Ordinal)..]
            : string.Empty);

        Assert.Equal(expected, StaticFileCacheControl.IsVersionedRequest(context.Request));
    }
}
