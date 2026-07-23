using System.Net;
using System.Net.Sockets;

namespace QueenZone.NewsAgent;

/// <summary>
/// <see cref="SocketsHttpHandler"/> that refuses connections to private/link-local/metadata
/// addresses after DNS resolution (SSRF protection for news discovery).
/// </summary>
public static class SsrfSafeSocketsHttpHandler
{
    public static SocketsHttpHandler Create(int maxAutomaticRedirections = 5)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = Math.Clamp(maxAutomaticRedirections, 0, 10),
            ConnectTimeout = TimeSpan.FromSeconds(15),
            ConnectCallback = ConnectCallbackAsync,
        };
        return handler;
    }

    internal static async ValueTask<Stream> ConnectCallbackAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var endPoint = await ResolveAllowedEndPointAsync(context.DnsEndPoint, cancellationToken)
            .ConfigureAwait(false);

        var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };

        try
        {
            await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static async Task<IPEndPoint> ResolveAllowedEndPointAsync(
        DnsEndPoint dnsEndPoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dnsEndPoint);

        if (OutboundUrlSafety.IsBlockedHostName(dnsEndPoint.Host))
        {
            throw new HttpRequestException(
                $"Refusing outbound connection to blocked host '{dnsEndPoint.Host}'.");
        }

        // Literal IP host
        if (IPAddress.TryParse(dnsEndPoint.Host, out var literal))
        {
            if (OutboundUrlSafety.IsBlockedAddress(literal))
            {
                throw new HttpRequestException(
                    $"Refusing outbound connection to blocked address '{literal}'.");
            }

            return new IPEndPoint(literal, dnsEndPoint.Port);
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(dnsEndPoint.Host, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new HttpRequestException(
                $"DNS resolution failed for host '{dnsEndPoint.Host}'.", ex);
        }

        foreach (var address in addresses)
        {
            if (!OutboundUrlSafety.IsBlockedAddress(address))
            {
                return new IPEndPoint(address, dnsEndPoint.Port);
            }
        }

        throw new HttpRequestException(
            $"Refusing outbound connection to '{dnsEndPoint.Host}': all resolved addresses are blocked (private/link-local/metadata).");
    }
}
