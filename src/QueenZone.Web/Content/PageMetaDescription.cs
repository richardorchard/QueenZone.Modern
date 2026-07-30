using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Builds unique, content-derived meta description text for public archive pages.
/// Values are plain text (no markup); Razor encodes them into <c>content</c> attributes.
/// </summary>
public static class PageMetaDescription
{
    /// <summary>Target length for search snippets (~155 characters).</summary>
    public const int MaxLength = 155;

    /// <summary>
    /// Sanitised plain-text excerpt from HTML or plain body content.
    /// </summary>
    public static string FromBody(string? body, int maxLength = MaxLength) =>
        LegacyArticleText.GetExcerpt(body, maxLength);

    /// <summary>
    /// Forum topic description: first post on the current page when present;
    /// otherwise a unique title-based fallback (never a board-wide generic).
    /// </summary>
    public static string ForForumTopic(
        string? firstPostBody,
        string title,
        string forumName,
        int page = 1)
    {
        var excerpt = FromBody(firstPostBody);
        if (!string.IsNullOrEmpty(excerpt))
        {
            return excerpt;
        }

        var safeTitle = string.IsNullOrWhiteSpace(title) ? "Forum thread" : title.Trim();
        var safeForum = string.IsNullOrWhiteSpace(forumName) ? "Queenzone forum" : forumName.Trim();
        var fallback = page <= 1
            ? $"{safeTitle} - read-only archive thread in {safeForum}."
            : $"{safeTitle} - page {page} in {safeForum}.";
        return FromBody(fallback, MaxLength);
    }

    /// <summary>
    /// Archive index description, unique per page number when paged.
    /// </summary>
    public static string ForArchiveIndex(string baseDescription, int page)
    {
        var baseText = string.IsNullOrWhiteSpace(baseDescription)
            ? "Queenzone archive."
            : baseDescription.Trim();

        if (page <= 1)
        {
            return baseText;
        }

        // Drop a trailing sentence period so "…Zone. - page 2." becomes "…Zone - page 2."
        // Use ASCII hyphen so Razor HTML encoding does not rewrite the separator in meta tags.
        var stem = baseText.TrimEnd();
        if (stem.EndsWith('.'))
        {
            stem = stem[..^1].TrimEnd();
        }

        var suffix = $" - page {page}.";
        var budget = Math.Max(20, MaxLength - suffix.Length);
        if (stem.Length > budget)
        {
            stem = stem[..budget].TrimEnd();
        }

        return stem + suffix;
    }
}
