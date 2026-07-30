namespace QueenZone.Web;

/// <summary>
/// Builds RSS 2.0 channel XML for public archive feeds.
/// Canonical feed paths: <c>/news/feed.rss</c>, <c>/articles/feed.rss</c>.
/// </summary>
public static class RssFeedBuilder
{
    /// <summary>Max items per feed (aligned with repository latest-count caps).</summary>
    public const int DefaultItemLimit = 50;

    public sealed record Item(
        string Title,
        string AbsoluteLink,
        string? Description,
        DateTime PublishedAtUtc);

    public static string Build(
        string channelTitle,
        string channelLink,
        string channelDescription,
        string selfAbsoluteUrl,
        IEnumerable<Item> items)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
        sb.AppendLine("<channel>");
        sb.AppendLine($"  <title>{EscapeXml(channelTitle)}</title>");
        sb.AppendLine($"  <link>{EscapeXml(channelLink)}</link>");
        sb.AppendLine($"  <description>{EscapeXml(channelDescription)}</description>");
        sb.AppendLine(
            $"  <atom:link href=\"{EscapeXml(selfAbsoluteUrl)}\" rel=\"self\" type=\"application/rss+xml\" />");
        sb.AppendLine($"  <lastBuildDate>{DateTime.UtcNow:R}</lastBuildDate>");

        foreach (var item in items)
        {
            var link = item.AbsoluteLink;
            sb.AppendLine("  <item>");
            sb.AppendLine($"    <title>{EscapeXml(item.Title)}</title>");
            sb.AppendLine($"    <link>{EscapeXml(link)}</link>");
            sb.AppendLine($"    <guid isPermaLink=\"true\">{EscapeXml(link)}</guid>");
            sb.AppendLine($"    <pubDate>{item.PublishedAtUtc.ToUniversalTime():R}</pubDate>");
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                sb.AppendLine($"    <description>{EscapeXml(item.Description)}</description>");
            }

            sb.AppendLine("  </item>");
        }

        sb.AppendLine("</channel>");
        sb.AppendLine("</rss>");
        return sb.ToString();
    }

    public static string EscapeXml(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
}
