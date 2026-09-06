namespace QueenZone.Web.Tests;

public sealed class ForumTextCleaningTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CleanForumText_ReturnsEmptyForMissingOrWhitespace(string? value) =>
        Assert.Equal(string.Empty, ForumTextCleaning.CleanForumText(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CleanForumTextOrNull_ReturnsNullForMissingOrWhitespace(string? value) =>
        Assert.Null(ForumTextCleaning.CleanForumTextOrNull(value));

    [Fact]
    public void CleanForumText_StripsTagsDecodesEntitiesAndCollapsesWhitespace()
    {
        var cleaned = ForumTextCleaning.CleanForumText("  Discuss &amp;  <b>request</b>\nitems  ");

        Assert.Equal("Discuss & request items", cleaned);
        Assert.Equal(cleaned, ForumTextCleaning.CleanForumTextOrNull("  Discuss &amp;  <b>request</b>\nitems  "));
    }
}
