using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace QueenZone.Web.Tests;

public sealed class ClientAddressTelemetryTests
{
    [Fact]
    public void EnrichCurrentActivity_PrefersCloudflareVisitorAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.64.0.10");
        context.Request.Headers[ClientAddressTelemetry.CloudflareConnectingIpHeader] = "203.0.113.42";
        using var activity = new Activity("request").Start();

        ClientAddressTelemetry.EnrichCurrentActivity(context, activity);

        Assert.Equal(
            "203.0.113.42",
            activity.GetTagItem(ClientAddressTelemetry.ClientAddressAttribute));
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("203.0.113.42, 198.51.100.7")]
    public void EnrichCurrentActivity_InvalidCloudflareHeaderFallsBackToForwardedAddress(string header)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.7");
        context.Request.Headers[ClientAddressTelemetry.CloudflareConnectingIpHeader] = header;
        using var activity = new Activity("request").Start();

        ClientAddressTelemetry.EnrichCurrentActivity(context, activity);

        Assert.Equal(
            "198.51.100.7",
            activity.GetTagItem(ClientAddressTelemetry.ClientAddressAttribute));
    }

    [Fact]
    public void EnrichCurrentActivity_NormalizesMappedIpv4Address()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ClientAddressTelemetry.CloudflareConnectingIpHeader] = "::ffff:203.0.113.42";
        using var activity = new Activity("request").Start();

        ClientAddressTelemetry.EnrichCurrentActivity(context, activity);

        Assert.Equal(
            "203.0.113.42",
            activity.GetTagItem(ClientAddressTelemetry.ClientAddressAttribute));
    }

    [Fact]
    public void EnrichCurrentActivity_NoAddressDoesNotAddTag()
    {
        var context = new DefaultHttpContext();
        using var activity = new Activity("request").Start();

        ClientAddressTelemetry.EnrichCurrentActivity(context, activity);

        Assert.Null(activity.GetTagItem(ClientAddressTelemetry.ClientAddressAttribute));
    }
}
