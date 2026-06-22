using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyStoryTextTests
{
    [Fact]
    public void GetExcerpt_StripsHtmlAndTruncatesLongText()
    {
        var excerpt = LegacyStoryText.GetExcerpt(
            "<p>First paragraph with <strong>emphasis</strong>.</p><p>Second paragraph continues the story.</p>",
            40);

        Assert.Equal("First paragraph with emphasis . Second p…", excerpt);
    }

    [Fact]
    public void GetExcerpt_ReturnsEmptyForBlankInput()
    {
        Assert.Equal(string.Empty, LegacyStoryText.GetExcerpt("   "));
    }
}