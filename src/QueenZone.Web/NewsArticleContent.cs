using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace QueenZone.Web;

public static partial class NewsArticleContent
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "div", "span", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a",
        "h2", "h3", "h4", "blockquote"
    };

    public static string GetDetailCanonicalPath(int id, string title) =>
        $"/news/{id}/{NewsRoutes.Slugify(title)}";

    public static bool IsSafePublicUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static string FormatBody(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        return LooksLikeHtml(body)
            ? SanitizeLegacyHtml(body)
            : WebUtility.HtmlEncode(body).Replace("\n", "<br>", StringComparison.Ordinal);
    }

    private static bool LooksLikeHtml(string value) =>
        HtmlTagRegex().IsMatch(value);

    private static string SanitizeLegacyHtml(string html)
    {
        var withoutScripts = ScriptBlockRegex().Replace(html, string.Empty);
        withoutScripts = StyleBlockRegex().Replace(withoutScripts, string.Empty);
        withoutScripts = DangerousTagRegex().Replace(withoutScripts, string.Empty);

        var builder = new StringBuilder(withoutScripts.Length);
        var index = 0;

        while (index < withoutScripts.Length)
        {
            var tagStart = withoutScripts.IndexOf('<', index);
            if (tagStart < 0)
            {
                AppendEncodedText(builder, withoutScripts[index..]);
                break;
            }

            AppendEncodedText(builder, withoutScripts[index..tagStart]);
            var tagEnd = withoutScripts.IndexOf('>', tagStart);
            if (tagEnd < 0)
            {
                AppendEncodedText(builder, withoutScripts[tagStart..]);
                break;
            }

            AppendSanitizedTag(builder, withoutScripts[tagStart..(tagEnd + 1)]);
            index = tagEnd + 1;
        }

        return builder.ToString();
    }

    private static void AppendEncodedText(StringBuilder builder, string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        builder.Append(WebUtility.HtmlEncode(text).Replace("\n", "<br>", StringComparison.Ordinal));
    }

    private static void AppendSanitizedTag(StringBuilder builder, string rawTag)
    {
        var trimmed = rawTag.Trim();
        if (trimmed.Length < 3)
        {
            return;
        }

        var inner = trimmed[1..^1].Trim();
        if (inner.Length == 0)
        {
            return;
        }

        var isClosing = inner.StartsWith('/');
        var tagName = isClosing ? inner[1..].Trim() : inner;
        var spaceIndex = tagName.IndexOfAny([' ', '\t', '\r', '\n']);
        if (spaceIndex >= 0)
        {
            tagName = tagName[..spaceIndex];
        }

        if (tagName.EndsWith('/'))
        {
            tagName = tagName[..^1];
        }

        if (!AllowedTags.Contains(tagName))
        {
            return;
        }

        if (isClosing)
        {
            if (string.Equals(tagName, "br", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            builder.Append("</");
            builder.Append(tagName.ToLowerInvariant());
            builder.Append('>');
            return;
        }

        if (string.Equals(tagName, "br", StringComparison.OrdinalIgnoreCase))
        {
            builder.Append("<br>");
            return;
        }

        if (string.Equals(tagName, "a", StringComparison.OrdinalIgnoreCase))
        {
            var href = ExtractHrefAttribute(inner);
            if (!IsSafePublicUrl(href))
            {
                return;
            }

            builder.Append("<a href=\"");
            builder.Append(WebUtility.HtmlEncode(href));
            builder.Append("\" rel=\"noopener noreferrer\">");
            return;
        }

        builder.Append('<');
        builder.Append(tagName.ToLowerInvariant());
        builder.Append('>');
    }

    private static string? ExtractHrefAttribute(string tagInner)
    {
        var match = HrefAttributeRegex().Match(tagInner);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Trim();
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return value;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("<script\\b[^>]*>[\\s\\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptBlockRegex();

    [GeneratedRegex("<style\\b[^>]*>[\\s\\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleBlockRegex();

    [GeneratedRegex("<\\s*/?\\s*(iframe|object|embed|form|input|meta|link|base|svg|math)\\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousTagRegex();

    [GeneratedRegex("""href\s*=\s*(?<value>"(?:[^"]*)"|'(?:[^']*)'|[^\s>]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex HrefAttributeRegex();
}