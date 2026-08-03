using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class OutboundUrlSafetyTests
{
    [Theory]
    [InlineData("https://www.queenonline.com/news/tour")]
    [InlineData("http://example.com/queen")]
    public void TryValidatePublicHttpUrl_accepts_safe_public_urls(string url)
    {
        var ok = OutboundUrlSafety.TryValidatePublicHttpUrl(url, out var error, out var normalized);

        Assert.True(ok, error);
        Assert.False(string.IsNullOrWhiteSpace(normalized));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("https://user:pass@example.com/secret")]
    [InlineData("http://localhost/admin")]
    [InlineData("http://127.0.0.1/meta")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://192.168.1.10/news")]
    [InlineData("http://10.0.0.5/news")]
    [InlineData("http://[::1]/")]
    public void TryValidatePublicHttpUrl_rejects_unsafe_urls(string url)
    {
        var ok = OutboundUrlSafety.TryValidatePublicHttpUrl(url, out var error, out var normalized);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Null(normalized);
    }

    [Fact]
    public void IsAllowedTextContentType_accepts_html_and_rejects_binary()
    {
        Assert.True(OutboundUrlSafety.IsAllowedTextContentType("text/html; charset=utf-8"));
        Assert.False(OutboundUrlSafety.IsAllowedTextContentType("application/octet-stream"));
        Assert.False(OutboundUrlSafety.IsAllowedTextContentType("image/png"));
    }
}
