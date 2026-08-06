using System.Net;
using System.Text.RegularExpressions;

namespace QueenZone.NewsAgent;

public sealed record ParsedArticlePage(
    string Title,
    string? Excerpt,
    string SourceName);

public static partial class NewsArticlePageParser
{
    private const int MaxExcerptLength = 1200;

    public static ParsedArticlePage Parse(string html, string pageUrl)
    {
        ArgumentNullException.ThrowIfNull(html);

        var title = FirstMatch(OgTitleRegex(), html)
            ?? FirstMatch(TwitterTitleRegex(), html)
            ?? FirstMatch(TitleTagRegex(), html)
            ?? DeriveTitleFromUrl(pageUrl);

        var excerpt = FirstMatch(MetaDescriptionRegex(), html)
            ?? FirstMatch(OgDescriptionRegex(), html)
            ?? FirstMatch(TwitterDescriptionRegex(), html);

        if (!string.IsNullOrWhiteSpace(excerpt))
        {
            excerpt = NormalizeWhitespace(Decode(excerpt));
            if (excerpt.Length > MaxExcerptLength)
            {
                excerpt = excerpt[..MaxExcerptLength].TrimEnd() + "…";
            }
        }

        var sourceName = Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : "Manual URL";

        return new ParsedArticlePage(
            NormalizeWhitespace(Decode(title)),
            string.IsNullOrWhiteSpace(excerpt) ? null : excerpt,
            sourceName);
    }

    private static string DeriveTitleFromUrl(string pageUrl)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
        {
            return "Submitted article";
        }

        var segment = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(segment))
        {
            return uri.Host;
        }

        return segment.Replace('-', ' ').Replace('_', ' ');
    }

    private static string? FirstMatch(Regex regex, string html)
    {
        var match = regex.Match(html);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Decode(string value) =>
        WebUtility.HtmlDecode(value);

    private static string NormalizeWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"<title[^>]*>(?<value>[\s\S]*?)</title>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TitleTagRegex();

    [GeneratedRegex(
        @"<meta\s+[^>]*property\s*=\s*[""']og:title[""'][^>]*content\s*=\s*(?<delimiter>[""'])(?<value>[\s\S]*?)\k<delimiter>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OgTitleRegex();

    [GeneratedRegex(
        @"<meta\s+[^>]*name\s*=\s*[""']twitter:title[""'][^>]*content\s*=\s*(?<delimiter>[""'])(?<value>[\s\S]*?)\k<delimiter>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TwitterTitleRegex();

    [GeneratedRegex(
        @"<meta\s+[^>]*name\s*=\s*[""']description[""'][^>]*content\s*=\s*(?<delimiter>[""'])(?<value>[\s\S]*?)\k<delimiter>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex(
        @"<meta\s+[^>]*property\s*=\s*[""']og:description[""'][^>]*content\s*=\s*(?<delimiter>[""'])(?<value>[\s\S]*?)\k<delimiter>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OgDescriptionRegex();

    [GeneratedRegex(
        @"<meta\s+[^>]*name\s*=\s*[""']twitter:description[""'][^>]*content\s*=\s*(?<delimiter>[""'])(?<value>[\s\S]*?)\k<delimiter>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TwitterDescriptionRegex();
}
