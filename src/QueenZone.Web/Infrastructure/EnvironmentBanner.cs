using System.Net;

namespace QueenZone.Web;

/// <summary>
/// Host-based non-production banner copy (#1407). Hide only on the public
/// production hosts. Never key off Azure resource names such as queenzone-dev.
/// </summary>
public static class EnvironmentBanner
{
    public const string LocalLabel = "LOCAL";
    public const string DevLabel = "DEV";
    public const string NonProdLabel = "NON-PROD";

    public static string? ResolveLabel(string? host)
    {
        var normalized = NormalizeHost(host);
        if (IsProductionHost(normalized))
        {
            return null;
        }

        if (IsLocalHost(normalized))
        {
            return LocalLabel;
        }

        if (string.Equals(normalized, "dev.queenzone.org", StringComparison.OrdinalIgnoreCase))
        {
            return DevLabel;
        }

        return NonProdLabel;
    }

    internal static string NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var value = host.Trim();
        var closeBracket = value.IndexOf(']');
        if (value.StartsWith('[') && closeBracket > 0)
        {
            value = value[1..closeBracket];
        }

        return value.TrimEnd('.');
    }

    internal static bool IsProductionHost(string host) =>
        string.Equals(host, "www.queenzone.org", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "queenzone.org", StringComparison.OrdinalIgnoreCase);

    internal static bool IsLocalHost(string host)
    {
        if (host.Length == 0)
        {
            return false;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }
}
