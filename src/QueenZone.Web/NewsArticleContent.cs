using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Ganss.Xss;
using QueenZone.Data;

namespace QueenZone.Web;

public static partial class NewsArticleContent
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    public static string GetDetailCanonicalPath(int id, string title, string? slug = null) =>
        $"/news/{id}/{NewsSlug.Resolve(title, slug)}";

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
            ? Sanitizer.Sanitize(body)
            : AutoLinkUrls(body);
    }

    /// <summary>
    /// Strips markup from a body field for use as plain text (e.g. a meta description),
    /// so legacy HTML-formatted fields don't leak literal tags into non-HTML contexts.
    /// </summary>
    public static string ToPlainText(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        var withoutTags = HtmlTagRegex().Replace(body, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    private static bool LooksLikeHtml(string value) =>
        HtmlTagRegex().IsMatch(value);

    private static string AutoLinkUrls(string plainText)
    {
        // Split on URLs, encode non-URL parts, wrap URLs in anchor tags.
        var parts = UrlRegex().Split(plainText);
        var sb = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            if (Uri.TryCreate(part, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var encoded = WebUtility.HtmlEncode(part);
                sb.Append($"<a href=\"{encoded}\" rel=\"noopener noreferrer\" target=\"_blank\">{encoded}</a>");
            }
            else
            {
                sb.Append(WebUtility.HtmlEncode(part).Replace("\n", "<br>", StringComparison.Ordinal));
            }
        }
        return sb.ToString();
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "div", "span", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a", "h2", "h3", "h4", "blockquote" })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add(Uri.UriSchemeHttp);
        sanitizer.AllowedSchemes.Add(Uri.UriSchemeHttps);

        sanitizer.PostProcessNode += (_, args) =>
        {
            if (args.Node is IElement element
                && string.Equals(element.TagName, "A", StringComparison.OrdinalIgnoreCase)
                && element.HasAttribute("href"))
            {
                element.SetAttribute("rel", "noopener noreferrer");
                element.SetAttribute("target", "_blank");
            }
        };

        return sanitizer;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(https?://\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();
}