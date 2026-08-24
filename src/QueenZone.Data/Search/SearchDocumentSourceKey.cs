using System.Globalization;

namespace QueenZone.Data;

/// <summary>
/// Parses <see cref="Entities.SearchDocumentEntity.SourceKey"/> values written by the search
/// reindex builder (<c>news:123</c>, <c>forum-thread:4521</c>, <c>article:some-slug</c>, …).
/// </summary>
public static class SearchDocumentSourceKey
{
    /// <summary>
    /// Returns the numeric id after the last colon when the suffix is an integer; otherwise
    /// <see langword="null"/> (slug keys such as <c>article:some-slug</c>).
    /// </summary>
    public static int? TryParseNumericId(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return null;
        }

        var separator = sourceKey.LastIndexOf(':');
        if (separator < 0 || separator == sourceKey.Length - 1)
        {
            return null;
        }

        var suffix = sourceKey.AsSpan(separator + 1);
        return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }
}
