namespace QueenZone.Data;

public sealed record QueenLink(
    int Id,
    string Title,
    string Url,
    string? Comment,
    int CategoryId,
    bool IsFeatured)
{
    public string DisplayHost => TryNormalizeHttpUrl(Url, out var uri) ? uri.Host : string.Empty;

    public static bool TryNormalizeHttpUrl(string candidate, out Uri normalizedUri)
    {
        normalizedUri = null!;
        var trimmed = candidate.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        var hasSchemeSeparator = trimmed.Contains("://", StringComparison.Ordinal);
        if (!hasSchemeSeparator && trimmed.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        if (!hasSchemeSeparator)
        {
            trimmed = "https://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        normalizedUri = uri;
        return true;
    }

    public QueenLink? ToPublicLink() =>
        TryNormalizeHttpUrl(Url, out var uri)
            ? this with { Url = uri.AbsoluteUri }
            : null;
}
