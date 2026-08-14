using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsArticleContentTests
{
    [Fact]
    public void FormatBody_EncodesPlainTextAndPreservesLineBreaks()
    {
        var result = NewsArticleContent.FormatBody("First line\nSecond line");

        Assert.Equal("First line<br>Second line", result);
    }

    [Fact]
    public void FormatBody_AllowsSafeLegacyHtmlAndStripsScripts()
    {
        var result = NewsArticleContent.FormatBody(
            "<script>alert('xss')</script><p>Queen <strong>news</strong></p>");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Queen <strong>news</strong></p>", result);
    }

    [Fact]
    public void FormatBody_RemovesUnsafeLinksAndKeepsSafeOnes()
    {
        var result = NewsArticleContent.FormatBody(
            """<a href="javascript:alert(1)">Bad</a><a href="https://example.com/article">Good</a>""");

        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"https://example.com/article\"", result);
    }

    [Fact]
    public void FormatBody_keeps_ai_generated_song_and_video_links_clickable()
    {
        var result = NewsArticleContent.FormatBody(
            """<p>Roger Taylor has released a new single. <a href="https://rogertaylor.lnk.to/iseeyounow">Listen to the song</a> or <a href="https://www.youtube.com/watch?v=KZivRNcsoJw">watch the video</a>.</p>""");

        Assert.Contains("href=\"https://rogertaylor.lnk.to/iseeyounow\"", result);
        Assert.Contains("href=\"https://www.youtube.com/watch?v=KZivRNcsoJw\"", result);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Count(result, "target=\"_blank\""));
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Count(result, "rel=\"noopener noreferrer\""));
    }

    [Theory]
    [InlineData("https://example.com/article", true)]
    [InlineData("http://example.com/article", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/local/path", false)]
    [InlineData(null, false)]
    public void IsSafePublicUrl_AcceptsOnlyHttpAndHttpsUrls(string? url, bool expected)
    {
        Assert.Equal(expected, NewsArticleContent.IsSafePublicUrl(url));
    }

    [Fact]
    public void GetDetailCanonicalPath_UsesSlugifiedTitle()
    {
        Assert.Equal(
            "/news/42/queen-live-aid-special",
            NewsArticleContent.GetDetailCanonicalPath(42, "Queen Live Aid special"));
    }
}
