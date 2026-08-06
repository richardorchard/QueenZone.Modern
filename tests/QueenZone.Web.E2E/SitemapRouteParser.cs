using System.Xml.Linq;

namespace QueenZone.Web.E2E;

internal static class SitemapRouteParser
{
    public static IReadOnlyList<string> ParseIndexPaths(string xml) =>
        ParseLocPaths(xml, "sitemapindex");

    public static IReadOnlyList<string> ParseUrlSetPaths(string xml) =>
        ParseLocPaths(xml, "urlset");

    public static string ResolveSectionName(string sitemapPath)
    {
        var fileName = sitemapPath.Split(['?', '#'], 2)[0].TrimStart('/');
        if (string.Equals(fileName, "sitemap-core.xml", StringComparison.OrdinalIgnoreCase))
        {
            return "core";
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(
                fileName,
                @"^sitemap-forum-\d+\.xml$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return "forum";
        }

        var sectionMatch = System.Text.RegularExpressions.Regex.Match(
            fileName,
            @"^sitemap-(.+)\.xml$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return sectionMatch.Success ? sectionMatch.Groups[1].Value : fileName;
    }

    private static IReadOnlyList<string> ParseLocPaths(string xml, string expectedRoot)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new FormatException("Sitemap XML has no root element.");
        if (!string.Equals(root.Name.LocalName, expectedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(
                $"Expected sitemap root '{expectedRoot}', found '{root.Name.LocalName}'.");
        }

        return doc.Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "loc", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value.Trim())
            .Where(v => v.Length > 0)
            .Select(ToPath)
            .ToList();
    }

    private static string ToPath(string locValue) =>
        Uri.TryCreate(locValue, UriKind.Absolute, out var uri) ? uri.PathAndQuery : locValue;
}
