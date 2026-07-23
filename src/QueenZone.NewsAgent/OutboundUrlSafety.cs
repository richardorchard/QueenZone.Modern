using System.Net;
using System.Net.Sockets;

namespace QueenZone.NewsAgent;

/// <summary>
/// SSRF guards for news-discovery outbound HTTP: scheme allowlist and
/// private/link-local/metadata IP blocking after DNS resolution.
/// </summary>
public static class OutboundUrlSafety
{
    public const int DefaultMaxResponseBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Ensures <paramref name="url"/> is an absolute http(s) URL suitable for discovery fetch.
    /// Prefer HTTPS; HTTP is allowed for legacy feeds but still subject to IP blocking on connect.
    /// </summary>
    public static void EnsureAllowedHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Outbound URL is required.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Outbound URL is not absolute: '{url}'.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            throw new InvalidOperationException(
                $"Outbound URL scheme '{uri.Scheme}' is not allowed. Use http or https.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException("Outbound URL host is required.");
        }

        // Literal IP in the URL — check immediately (no DNS required).
        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal)
            && IsBlockedAddress(literal))
        {
            throw new InvalidOperationException(
                $"Outbound URL host resolves to a blocked address family ({literal}).");
        }

        if (IsBlockedHostName(uri.DnsSafeHost))
        {
            throw new InvalidOperationException(
                $"Outbound URL host '{uri.DnsSafeHost}' is not allowed.");
        }
    }

    public static bool IsBlockedHostName(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("metadata", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Azure IMDS host
        if (host.Equals("169.254.169.254", StringComparison.OrdinalIgnoreCase)
            || host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsBlockedAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.None)
            || address.Equals(IPAddress.Broadcast))
        {
            return true;
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            // 0.0.0.0/8
            if (bytes[0] == 0)
            {
                return true;
            }

            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 127.0.0.0/8 already covered by IsLoopback for 127.0.0.1; keep full range.
            if (bytes[0] == 127)
            {
                return true;
            }

            // 169.254.0.0/16 link-local + cloud metadata
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 100.64.0.0/10 carrier-grade NAT
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
            {
                return true;
            }
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6
            && address.IsIPv4MappedToIPv6)
        {
            return IsBlockedAddress(address.MapToIPv4());
        }

        return false;
    }
}
