using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PageMetaDescriptionTests
{
    [Fact]
    public void FromBody_StripsHtmlAndTruncatesToMaxLength()
    {
        var longHtml = "<p>" + new string('a', 200) + " <strong>bold</strong> text</p>";

        var result = PageMetaDescription.FromBody(longHtml);

        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain("strong", result);
        Assert.True(result.Length <= PageMetaDescription.MaxLength + 1); // ellipsis may add one
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void FromBody_DecodesEntitiesWithoutDoubleDecoding()
    {
        var result = PageMetaDescription.FromBody("<p>5 &amp;lt; 10 &amp; Queen</p>");

        Assert.Equal("5 &lt; 10 & Queen", result);
    }

    [Fact]
    public void ForForumTopic_UsesFirstPostPlainText()
    {
        var result = PageMetaDescription.ForForumTopic(
            "Where would you put <strong>A Night at the Opera</strong> in the ranking?",
            "Ranking every studio album",
            "The Music");

        Assert.Equal("Where would you put A Night at the Opera in the ranking?", result);
        Assert.DoesNotContain("<strong>", result);
    }

    [Fact]
    public void ForForumTopic_FallsBackToUniqueTitleWhenBodyEmpty()
    {
        var a = PageMetaDescription.ForForumTopic(null, "Thread A", "The Music");
        var b = PageMetaDescription.ForForumTopic("   ", "Thread B", "The Music");

        Assert.Contains("Thread A", a);
        Assert.Contains("Thread B", b);
        Assert.NotEqual(a, b);
        Assert.DoesNotContain("Read-only Queenzone forum archive thread in", a);
    }

    [Fact]
    public void ForArchiveIndex_AppendsPageNumberAfterFirstPage()
    {
        Assert.Equal(
            "The latest Queen news and stories from QueenZone.",
            PageMetaDescription.ForArchiveIndex("The latest Queen news and stories from QueenZone.", 1));

        Assert.Equal(
            "The latest Queen news and stories from QueenZone - page 2.",
            PageMetaDescription.ForArchiveIndex("The latest Queen news and stories from QueenZone.", 2));
    }

    [Fact]
    public void ForArchiveIndex_KeepsPagedDescriptionNearMaxLength()
    {
        var longBase = new string('x', 200);

        var result = PageMetaDescription.ForArchiveIndex(longBase, 3);

        Assert.EndsWith(" - page 3.", result);
        Assert.True(result.Length <= PageMetaDescription.MaxLength);
    }
}
