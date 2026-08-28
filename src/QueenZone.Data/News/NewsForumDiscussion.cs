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

    public static bool MatchesNewsCategory(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return string.Equals(name.Trim(), CategoryName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(NewsSlug.Slugify(name), CategorySlug, StringComparison.OrdinalIgnoreCase);
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
