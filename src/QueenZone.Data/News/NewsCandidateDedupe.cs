using System.Security.Cryptography;
using System.Text;

namespace QueenZone.Data;

public static class NewsCandidateDedupe
{
    private static readonly HashSet<string> TrackingQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "fbclid",
        "gclid",
        "mc_cid",
        "mc_eid",
        "ref",
        "ref_src",
        "utm_campaign",
        "utm_content",
        "utm_id",
        "utm_medium",
        "utm_source",
        "utm_term"
    };

    public static string NormalizeCanonicalUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url.Trim().ToLowerInvariant();
        }

        var builder = new UriBuilder(uri)
        {
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty
        };

        if ((builder.Scheme == "http" && builder.Port == 80) || (builder.Scheme == "https" && builder.Port == 443))
        {
            builder.Port = -1;
        }

        var path = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrEmpty(path) ? "/" : path;

        if (string.IsNullOrEmpty(builder.Query))
        {
            return builder.Uri.AbsoluteUri;
        }

        var query = ParseQuery(builder.Query);
        var filtered = query
            .Where(pair => !TrackingQueryKeys.Contains(pair.Key) && !pair.Key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ThenBy(pair => pair.Value, StringComparer.Ordinal)
            .ToList();

        builder.Query = filtered.Count == 0
            ? string.Empty
            : string.Join("&", filtered.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return builder.Uri.AbsoluteUri;
    }

    public static string ComputeUrlHash(string canonicalUrl) =>
        ComputeSha256Hex(NormalizeCanonicalUrl(canonicalUrl));

    public static string ComputeContentHash(string? title, string? excerpt)
    {
        var normalizedTitle = NormalizeText(title);
        var normalizedExcerpt = NormalizeText(excerpt);
        return ComputeSha256Hex($"{normalizedTitle}\n{normalizedExcerpt}");
    }

    private static string NormalizeText(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string ComputeSha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseQuery(string query)
    {
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrEmpty(trimmed))
        {
            yield break;
        }

        foreach (var segment in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf('=');
            if (separator < 0)
            {
                yield return new KeyValuePair<string, string>(Uri.UnescapeDataString(segment), string.Empty);
                continue;
            }

            var key = Uri.UnescapeDataString(segment[..separator]);
            var value = Uri.UnescapeDataString(segment[(separator + 1)..]);
            yield return new KeyValuePair<string, string>(key, value);
        }
    }
}
