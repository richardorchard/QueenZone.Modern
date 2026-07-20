using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ArticleBodyValidationTests
{
    [Fact]
    public void CountVisibleChars_ReturnsZero_ForNullOrEmpty()
    {
        Assert.Equal(0, EfArticleSubmissionRepository.CountVisibleChars(null));
        Assert.Equal(0, EfArticleSubmissionRepository.CountVisibleChars(string.Empty));
    }

    [Fact]
    public void CountVisibleChars_StripsHtmlTags()
    {
        var html = "<p><strong>Hello</strong> world.</p>";
        var count = EfArticleSubmissionRepository.CountVisibleChars(html);
        Assert.Equal("Hello world.".Length, count);
    }

    [Fact]
    public void CountVisibleChars_DecodesHtmlEntities()
    {
        var html = "Rock &amp; Roll";
        var count = EfArticleSubmissionRepository.CountVisibleChars(html);
        Assert.Equal("Rock & Roll".Length, count);
    }

    [Fact]
    public void CountVisibleChars_CollapseWhitespace()
    {
        var html = "<p>A</p>   <p>B</p>";
        var count = EfArticleSubmissionRepository.CountVisibleChars(html);
        Assert.Equal("A B".Length, count);
    }

    [Theory]
    [InlineData(299, false)]
    [InlineData(300, true)]
    [InlineData(301, true)]
    public void MinBodyCheck_EnforcesThreshold(int charCount, bool shouldPass)
    {
        var text = new string('x', charCount);
        var count = EfArticleSubmissionRepository.CountVisibleChars(text);
        Assert.Equal(charCount >= EfArticleSubmissionRepository.MinBodyVisibleChars, shouldPass);
        if (shouldPass)
        {
            Assert.True(count >= EfArticleSubmissionRepository.MinBodyVisibleChars);
        }
        else
        {
            Assert.True(count < EfArticleSubmissionRepository.MinBodyVisibleChars);
        }
    }
}
