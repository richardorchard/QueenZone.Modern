using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using Ganss.Xss;
using Microsoft.Extensions.Options;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Server-side sanitization for user-generated rich HTML (forum, future UGC articles).
/// Separate from <see cref="NewsArticleContent"/> so public news allowlists do not expand by accident.
/// </summary>
public sealed class UgcHtml(IOptions<BlobUploadOptions> blobUploadOptions)
{
    private readonly BlobUploadOptions blobOptions = blobUploadOptions.Value;

    public string Sanitize(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        return CreateSanitizer().Sanitize(html);
    }

    public bool IsAllowedImageSrc(string? src)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return false;
        }

        if (!Uri.TryCreate(src.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        var host = uri.Host;

        // Configured CDN / public base for UGC.
        if (!string.IsNullOrWhiteSpace(blobOptions.PublicBaseUrl)
            && Uri.TryCreate(blobOptions.PublicBaseUrl.Trim(), UriKind.Absolute, out var publicUri)
            && (string.Equals(host, publicUri.Host, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + publicUri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Direct Azure blob URLs only when the path references a known UGC container.
        if (host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase)
            && PathContainsUgcContainer(uri.AbsolutePath))
        {
            return true;
        }

        return false;
    }

    private static bool PathContainsUgcContainer(string path)
    {
        foreach (var container in BlobUploadContainers.All)
        {
            if (path.Contains("/" + container + "/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/" + container + "/", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/" + container, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [ExcludeFromCodeCoverage(Justification = "HtmlSanitizer wiring; covered via Sanitize tests.")]
    private HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "div", "span", "strong", "b", "em", "i", "u",
                     "ul", "ol", "li", "a", "h2", "h3", "h4", "blockquote", "img",
                 })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.AllowedAttributes.Add("alt");
        sanitizer.AllowedAttributes.Add("title");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add(Uri.UriSchemeHttp);
        sanitizer.AllowedSchemes.Add(Uri.UriSchemeHttps);

        sanitizer.AllowedCssProperties.Clear();

        sanitizer.PostProcessNode += (_, args) =>
        {
            if (args.Node is not IElement element)
            {
                return;
            }

            if (string.Equals(element.TagName, "A", StringComparison.OrdinalIgnoreCase)
                && element.HasAttribute("href"))
            {
                element.SetAttribute("rel", "noopener noreferrer");
                element.SetAttribute("target", "_blank");
            }

            if (string.Equals(element.TagName, "IMG", StringComparison.OrdinalIgnoreCase))
            {
                var src = element.GetAttribute("src");
                if (!IsAllowedImageSrc(src))
                {
                    element.Remove();
                }
            }
        };

        return sanitizer;
    }
}
