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
}
