using System.Security.Cryptography;

namespace QueenZone.Web;

/// <summary>
/// Generates and caches a per-request nonce used to allow specific inline
/// &lt;script&gt; blocks under an enforcing Content-Security-Policy.
/// </summary>
public static class CspNonce
{
    private const string ItemKey = "QueenZone.CspNonce";

    /// <summary>
    /// Returns the nonce for the current request, generating and caching one
    /// on first access so the header and any rendered markup agree.
    /// </summary>
    public static string Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var existing) && existing is string nonce)
        {
            return nonce;
        }

        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        // Base64Url (RFC 4648 §5) instead of standard Base64: '+' and '/' would otherwise
        // round-trip through HTML attribute encoding as '&#x2B;'/'&#x2F;', which is harmless to
        // browsers but makes the header value and the rendered markup awkward to compare directly.
        nonce = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        context.Items[ItemKey] = nonce;
        return nonce;
    }
}

public static class CspNonceHttpContextExtensions
{
    /// <summary>Convenience accessor for use in Razor views: @Context.GetCspNonce()</summary>
    public static string GetCspNonce(this HttpContext context) => CspNonce.Get(context);
}
