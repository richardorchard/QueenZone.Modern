using System.Diagnostics;
using System.Net;

namespace QueenZone.Web;

internal static class ClientAddressTelemetry
{
    internal const string ClientAddressAttribute = "client.address";
    internal const string CloudflareConnectingIpHeader = "CF-Connecting-IP";

    public static IApplicationBuilder UseClientAddressTelemetry(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static (context, next) =>
        {
            EnrichCurrentActivity(context);
            return next(context);
        });
    }

    internal static void EnrichCurrentActivity(HttpContext context, Activity? activity = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        activity ??= Activity.Current;
        var clientAddress = ResolveClientAddress(context);
        if (activity is null || clientAddress is null)
        {
            return;
        }

        activity.SetTag(ClientAddressAttribute, clientAddress.ToString());
    }

    internal static IPAddress? ResolveClientAddress(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Headers.TryGetValue(CloudflareConnectingIpHeader, out var values)
            && values.Count == 1
            && IPAddress.TryParse(values[0], out var cloudflareAddress))
        {
            return Normalize(cloudflareAddress);
        }

        return context.Connection.RemoteIpAddress is { } remoteAddress
            ? Normalize(remoteAddress)
            : null;
    }

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
