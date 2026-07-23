using System.Net;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class OutboundUrlSafetyTests
{
    [Theory]
    [InlineData("https://www.queenonline.com/feed/")]
    [InlineData("http://example.com/rss.xml")]
    public void EnsureAllowedHttpUrl_accepts_public_http_urls(string url)
    {
        var exception = Record.Exception(() => OutboundUrlSafety.EnsureAllowedHttpUrl(url));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/x")]
    [InlineData("gopher://example.com/")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void EnsureAllowedHttpUrl_rejects_bad_schemes_and_relative(string url)
    {
        Assert.ThrowsAny<Exception>(() => OutboundUrlSafety.EnsureAllowedHttpUrl(url));
    }

    [Theory]
    [InlineData("http://127.0.0.1/")]
    [InlineData("https://192.168.1.1/")]
    [InlineData("http://10.0.0.5/path")]
    [InlineData("http://172.16.0.1/")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://localhost/admin")]
    [InlineData("http://metadata.google.internal/")]
    public void EnsureAllowedHttpUrl_rejects_literal_private_and_metadata_hosts(string url)
    {
        Assert.Throws<InvalidOperationException>(() => OutboundUrlSafety.EnsureAllowedHttpUrl(url));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("172.20.0.1", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("100.64.1.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    public void IsBlockedAddress_matches_expected_ipv4(string ip, bool blocked)
    {
        Assert.Equal(blocked, OutboundUrlSafety.IsBlockedAddress(IPAddress.Parse(ip)));
    }

    [Fact]
    public void IsBlockedAddress_blocks_loopback_and_unique_local_ipv6()
    {
        Assert.True(OutboundUrlSafety.IsBlockedAddress(IPAddress.IPv6Loopback));
        Assert.True(OutboundUrlSafety.IsBlockedAddress(IPAddress.Parse("fe80::1")));
        Assert.True(OutboundUrlSafety.IsBlockedAddress(IPAddress.Parse("fc00::1")));
    }

    [Fact]
    public async Task ResolveAllowedEndPointAsync_rejects_blocked_literal()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            SsrfSafeSocketsHttpHandler.ResolveAllowedEndPointAsync(
                new System.Net.DnsEndPoint("127.0.0.1", 80),
                CancellationToken.None));
    }

    [Fact]
    public async Task ResolveAllowedEndPointAsync_accepts_public_literal()
    {
        var endPoint = await SsrfSafeSocketsHttpHandler.ResolveAllowedEndPointAsync(
            new System.Net.DnsEndPoint("8.8.8.8", 443),
            CancellationToken.None);

        Assert.Equal(IPAddress.Parse("8.8.8.8"), endPoint.Address);
        Assert.Equal(443, endPoint.Port);
    }
}
