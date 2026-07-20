using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsArticleContentSanitizationTests
{
    [Fact]
    public void FormatBody_RemovesUnclosedTag()
    {
        var result = NewsArticleContent.FormatBody("<p>Queen news<script>alert(1)");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatBody_StripsScriptRegardlessOfTagCase()
    {
        var result = NewsArticleContent.FormatBody("<P>Queen <SCRIPT>alert(1)</SCRIPT>news</P>");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queen", result);
        Assert.Contains("news", result);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    public void FormatBody_RemovesUnsafeUrlSchemesFromLinks(string unsafeHref)
    {
        var result = NewsArticleContent.FormatBody($"""<a href="{unsafeHref}">Click me</a>""");

        Assert.DoesNotContain(unsafeHref, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatBody_RemovesNestedScriptInsideAllowedTag()
    {
        var result = NewsArticleContent.FormatBody("<p>Queen <script>alert(document.cookie)</script>news</p>");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cookie", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queen", result);
        Assert.Contains("news", result);
    }

    [Fact]
    public void FormatBody_RemovesStyleBlock()
    {
        var result = NewsArticleContent.FormatBody("<style>body { background: url(javascript:alert(1)) }</style><p>Queen news</p>");

        Assert.DoesNotContain("background", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queen news", result);
    }

    [Theory]
    [InlineData("""<p onmouseover="alert(1)">Queen news</p>""")]
    [InlineData("""<a href="https://example.com" onclick="alert(1)">Queen news</a>""")]
    [InlineData("""<div onerror="alert(1)">Queen news</div>""")]
    public void FormatBody_StripsEventHandlerAttributesFromAllowedTags(string maliciousHtml)
    {
        var result = NewsArticleContent.FormatBody(maliciousHtml);

        Assert.DoesNotContain("onmouseover", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatBody_RemovesDisallowedTagsLikeIframeAndForm()
    {
        var result = NewsArticleContent.FormatBody(
            """<iframe src="https://evil.example.com"></iframe><form action="/x"><input type="text"></form><p>Queen news</p>""");

        Assert.DoesNotContain("iframe", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<form", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<input", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queen news", result);
    }

    [Fact]
    public void FormatBody_StripsClassAndStyleAttributesFromAllowedTags()
    {
        var result = NewsArticleContent.FormatBody(
            """<p class="evil" style="background:url(javascript:alert(1))">Queen news</p>""");

        Assert.DoesNotContain("class=", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queen news", result);
    }

    [Fact]
    public void FormatBody_AddsNoopenerNoreferrerToSafeLinks()
    {
        var result = NewsArticleContent.FormatBody("""<a href="https://example.com/article">Read more</a>""");

        Assert.Contains("rel=\"noopener noreferrer\"", result);
    }

    [Fact]
    public void FormatBody_DoesNotDoubleDecodeLegacyEntities()
    {
        var result = NewsArticleContent.FormatBody("<p>5 &amp;lt; 10</p>");

        Assert.DoesNotContain("<10", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&amp;lt;", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatBody_PreservesAllowedFormattingTags()
    {
        var html = "<p><strong>bold</strong> and <em>italic</em></p>" +
                   "<ul><li>item</li></ul>" +
                   "<a href=\"https://example.com\">link</a>";

        var result = NewsArticleContent.FormatBody(html);

        Assert.Contains("<strong>bold</strong>", result);
        Assert.Contains("<em>italic</em>", result);
        Assert.Contains("<ul>", result);
        Assert.Contains("<li>item</li>", result);
        Assert.Contains("href=\"https://example.com\"", result);
    }

    [Fact]
    public void FormatBody_PreservesUgcBlobImageAndStripsExternalImage()
    {
        var ugcSrc = "https://mystorage.blob.core.windows.net/ugc-news/photo.jpg";
        var externalSrc = "https://evil.example.com/tracker.png";
        var html = $"<p><img src=\"{ugcSrc}\" alt=\"queen\"> and <img src=\"{externalSrc}\" alt=\"bad\"></p>";

        var result = NewsArticleContent.FormatBody(html);

        Assert.Contains(ugcSrc, result);
        Assert.DoesNotContain(externalSrc, result);
    }

    [Fact]
    public void FormatBody_PreservesUgcProxyImagePath()
    {
        var proxySrc = "/ugc/news/abc123.jpg";
        var html = $"<img src=\"{proxySrc}\" alt=\"queen\">";

        var result = NewsArticleContent.FormatBody(html);

        Assert.Contains(proxySrc, result);
    }

    [Fact]
    public void FormatBody_StripsScriptOnclickAndIframeFromRichHtml()
    {
        var html = "<p onclick=\"alert(1)\">text</p>" +
                   "<script>evil()</script>" +
                   "<iframe src=\"https://evil.example.com\"></iframe>";

        var result = NewsArticleContent.FormatBody(html);

        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", result);
    }

    [Fact]
    public void FormatBody_ReturnsPlainTextBodyUnchangedAsAutoLinked()
    {
        var plainText = "Queen released a new album.";

        var result = NewsArticleContent.FormatBody(plainText);

        Assert.Contains("Queen released a new album.", result);
        Assert.DoesNotContain("<p>", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/ugc/news/img.jpg")]
    [InlineData("/ugc/forum/img.jpg")]
    [InlineData("https://storage.blob.core.windows.net/ugc-news/img.jpg")]
    [InlineData("https://storage.blob.core.windows.net/ugc-forum/img.jpg")]
    public void IsAllowedNewsImageSrc_AllowsUgcPaths(string src)
    {
        Assert.True(NewsArticleContent.IsAllowedNewsImageSrc(src));
    }

    [Theory]
    [InlineData("https://evil.example.com/img.jpg")]
    [InlineData("https://storage.blob.core.windows.net/public-photos/img.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    [InlineData(null)]
    public void IsAllowedNewsImageSrc_BlocksNonUgcPaths(string? src)
    {
        Assert.False(NewsArticleContent.IsAllowedNewsImageSrc(src));
    }
}
