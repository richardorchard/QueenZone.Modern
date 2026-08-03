using QueenZone.Tools.BbCode;

namespace QueenZone.Tools.Tests;

public class BbCodeConverterTests
{
    [Fact]
    public void Convert_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, BbCodeConverter.Convert(string.Empty));
        Assert.Equal(string.Empty, BbCodeConverter.Convert(null!));
    }

    [Fact]
    public void Convert_Bold()
    {
        var html = BbCodeConverter.Convert("[b]hello[/b] world");
        Assert.Equal("<strong>hello</strong> world", html);
    }

    [Fact]
    public void Convert_Italic()
    {
        var html = BbCodeConverter.Convert("cocaine is the rich man's drug [i]well, Brian, perhaps not[/i]");
        Assert.Contains("<em>well, Brian, perhaps not</em>", html);
    }

    [Fact]
    public void Convert_Underline()
    {
        Assert.Equal("<u>x</u>", BbCodeConverter.Convert("[u]x[/u]"));
    }

    [Fact]
    public void Convert_TagsAreCaseInsensitive()
    {
        var html = BbCodeConverter.Convert("[B]shout[/B]");
        Assert.Equal("<strong>shout</strong>", html);
    }

    [Fact]
    public void Convert_RealLegacyQuoteFormat()
    {
        // Dominant format found in the ModernForumPost.BodyHtml corpus.
        var html = BbCodeConverter.Convert(
            "[QUOTE][QUOTENAME]Biggzy10 wrote: [/QUOTENAME]Wasnt John a heavey drinker[/QUOTE] Yes he was.");

        Assert.Equal(
            "<blockquote class=\"qz-bbcode-quote\">"
            + "<div class=\"qz-bbcode-quote-author\"><strong>Biggzy10 wrote: </strong></div>"
            + "Wasnt John a heavey drinker"
            + "</blockquote> Yes he was.",
            html);
    }

    [Fact]
    public void Convert_BareLowercaseQuote()
    {
        var html = BbCodeConverter.Convert("[quote]hi there[/quote]");
        Assert.Equal("<blockquote class=\"qz-bbcode-quote\">hi there</blockquote>", html);
    }

    [Fact]
    public void Convert_NestedQuotes()
    {
        var html = BbCodeConverter.Convert("[quote]outer [quote]inner[/quote] tail[/quote]");
        Assert.Equal(
            "<blockquote class=\"qz-bbcode-quote\">outer "
            + "<blockquote class=\"qz-bbcode-quote\">inner</blockquote> tail</blockquote>",
            html);
    }

    [Fact]
    public void Convert_UnclosedTag_StillRendersRatherThanFailing()
    {
        var html = BbCodeConverter.Convert("[b]bold to the end");
        Assert.Equal("<strong>bold to the end</strong>", html);
    }

    [Fact]
    public void Convert_OrphanClosingTag_TreatedAsLiteralText()
    {
        var html = BbCodeConverter.Convert("no open tag here[/quote] still text");
        Assert.Equal("no open tag here[/quote] still text", html);
    }

    [Fact]
    public void Convert_MismatchedCrossedTags_DoesNotThrow()
    {
        var exception = Record.Exception(() => BbCodeConverter.Convert("[b][i]both[/b][/i]"));
        Assert.Null(exception);
    }

    [Fact]
    public void Convert_UnknownTag_PreservedAsLiteralText()
    {
        var html = BbCodeConverter.Convert("[code]not supported[/code]");
        Assert.Equal("[code]not supported[/code]", html);
    }

    [Fact]
    public void Convert_UrlWithoutAttribute_LinksToItsOwnText()
    {
        var html = BbCodeConverter.Convert("[url]https://example.com/page[/url]");
        Assert.Equal("<a href=\"https://example.com/page\">https://example.com/page</a>", html);
    }

    [Fact]
    public void Convert_UrlWithAttributeAndDisplayText()
    {
        var html = BbCodeConverter.Convert("[url=https://example.com]click here[/url]");
        Assert.Equal("<a href=\"https://example.com\">click here</a>", html);
    }

    [Fact]
    public void Convert_UrlWithDisallowedScheme_FallsBackToPlainText()
    {
        var html = BbCodeConverter.Convert("[url=javascript:alert(1)]click me[/url]");
        Assert.DoesNotContain("<a", html, StringComparison.Ordinal);
        Assert.Contains("click me", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Convert_HtmlEscapesRawText()
    {
        var html = BbCodeConverter.Convert("<script>alert(1)</script> & \"quotes\"");
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&amp;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Convert_NewlinesBecomeBreaks()
    {
        var html = BbCodeConverter.Convert("line one\nline two");
        Assert.Equal("line one<br>line two", html);
    }

    [Fact]
    public void Convert_DeeplyNestedQuotes_DoesNotThrowAndCapsDepth()
    {
        var nested = string.Concat(Enumerable.Repeat("[quote]", 30)) + "core"
            + string.Concat(Enumerable.Repeat("[/quote]", 30));

        var exception = Record.Exception(() => BbCodeConverter.Convert(nested));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("no bbcode here", false)]
    [InlineData("has [quote]a quote[/quote]", true)]
    [InlineData("[B]bold[/B]", true)]
    [InlineData("[url]http://x.com[/url]", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsBbCode_DetectsMarkers(string? text, bool expected)
    {
        Assert.Equal(expected, BbCodeConverter.ContainsBbCode(text));
    }

    [Theory]
    [InlineData("[CHORUS]\nsome lyrics\n[CHORUS]")]
    [InlineData("repeat the bridge [x3]")]
    [InlineData("[verse 1]\nmore lyrics")]
    public void ContainsBbCode_IgnoresNonBbCodeBracketedText(string text)
    {
        // Song lyrics posted to the forum are full of bracket markers like [CHORUS]/[x3]
        // that look tag-like but are not BBCode — must not be treated as candidates.
        Assert.False(BbCodeConverter.ContainsBbCode(text));
    }
}
