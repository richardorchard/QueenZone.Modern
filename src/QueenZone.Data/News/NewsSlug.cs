using System.Text.RegularExpressions;

namespace QueenZone.Data;

public static partial class NewsSlug
{
    public const int MaxLength = 200;

    public static string Resolve(string title, string? slugOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(slugOverride))
        {
            return Slugify(slugOverride);
        }

        return Slugify(title);
    }

    public static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var replaced = NonAlphaNumericRegex().Replace(lower, "-");
        var slug = DuplicateDashRegex().Replace(replaced, "-").Trim('-');
        if (slug.Length <= MaxLength)
        {
            return slug;
        }

        return slug[..MaxLength].Trim('-');
    }

    public static string ResolveForArticle(NewsItem item) =>
        string.IsNullOrWhiteSpace(item.Slug) ? Slugify(item.Title) : item.Slug;

    public static string ResolveForArticle(AdminNewsArticle article) =>
        string.IsNullOrWhiteSpace(article.Slug) ? Slugify(article.Title) : article.Slug;

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateDashRegex();
}
