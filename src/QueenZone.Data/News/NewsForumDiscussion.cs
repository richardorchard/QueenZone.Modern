using System.Net;
using System.Text.RegularExpressions;

namespace QueenZone.Data;

/// <summary>
/// Shared constants and helpers for the first-publish News-forum topic link (ADR 0016).
/// </summary>
public static partial class NewsForumDiscussion
{
    public const string CategorySlug = "news";

    public const string CategoryName = "News";

    public const string TheMusicName = "The Music";

    public const string SystemMemberEmail = "queenzone@queenzone.internal";

    public const string SystemMemberDisplayName = "QueenZone";

    public const string PublicArticleOrigin = "https://www.queenzone.org";

    public const int OpeningExcerptMaxLength = 400;

    public const int PreviewReplyCount = 2;

    public const int PreviewExcerptMaxLength = 200;

    public static bool MatchesNewsCategory(string? name) =>
        MatchesCategorySlug(name, CategorySlug) || MatchesCategoryName(name, CategoryName);

    /// <summary>
    /// Slug first, then case-insensitive name. Never returns The Music.
    /// </summary>
    public static T? FindExistingCategory<T>(
        IEnumerable<T> categories,
        Func<T, string> nameSelector,
        string slug,
        string name)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(nameSelector);

        var list = categories as IList<T> ?? categories.ToList();
        foreach (var category in list)
        {
            if (MatchesCategorySlug(nameSelector(category), slug))
            {
                return category;
            }
        }

        foreach (var category in list)
        {
            if (MatchesCategoryName(nameSelector(category), name))
            {
                return category;
            }
        }

        return default;
    }

    public static bool MatchesCategorySlug(string? name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug) || IsTheMusic(name))
        {
            return false;
        }

        return string.Equals(NewsSlug.Slugify(name), NewsSlug.Slugify(slug), StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesCategoryName(string? name, string expected)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(expected) || IsTheMusic(name))
        {
            return false;
        }

        return string.Equals(name.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTheMusic(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return string.Equals(name.Trim(), TheMusicName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(NewsSlug.Slugify(name), NewsSlug.Slugify(TheMusicName), StringComparison.OrdinalIgnoreCase);
    }

    public static string TruncatePlain(string? value, int maxLength)
    {
        var text = CollapseWhitespace(StripTags(value ?? string.Empty));
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength].TrimEnd();
    }

    public static string StripTags(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return WebUtility.HtmlDecode(TagRegex().Replace(value, string.Empty)) ?? string.Empty;
    }

    private static string CollapseWhitespace(string value) =>
        WhitespaceRegex().Replace(value, " ").Trim();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
