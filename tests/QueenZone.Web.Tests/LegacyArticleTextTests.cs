using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyArticleTextTests
{
    [Fact]
    public void GetExcerpt_StripsHtmlAndTruncatesLongText()
    {
        var excerpt = LegacyArticleText.GetExcerpt(
            "<p>First paragraph with <strong>emphasis</strong>.</p><p>Second paragraph continues the article.</p>",
            40);

        Assert.Equal("First paragraph with emphasis . Second p…", excerpt);
    }

    [Fact]
    public void GetExcerpt_ReturnsEmptyForBlankInput()
    {
        Assert.Equal(string.Empty, LegacyArticleText.GetExcerpt("   "));
    }
}