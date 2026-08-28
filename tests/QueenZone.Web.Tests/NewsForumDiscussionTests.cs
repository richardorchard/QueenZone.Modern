using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsForumDiscussionTests
{
    [Fact]
    public void MatchesNewsCategory_AcceptsNameOrSlug_AndRejectsTheMusic()
    {
        Assert.True(NewsForumDiscussion.MatchesNewsCategory("News"));
        Assert.True(NewsForumDiscussion.MatchesNewsCategory("news"));
        Assert.False(NewsForumDiscussion.MatchesNewsCategory("The Music"));
        Assert.True(NewsForumDiscussion.IsTheMusic("The Music"));
        Assert.True(NewsForumDiscussion.IsTheMusic("the-music"));
        Assert.False(NewsForumDiscussion.IsTheMusic("News"));
    }

    [Fact]
    public void FindExistingCategory_PrefersSlugThenName_AndNeverReturnsTheMusic()
    {
        var categories = new[]
        {
            new NamedCategory("The Music"),
            new NamedCategory("News Desk"),
            new NamedCategory("NEWS!"),
            new NamedCategory("News"),
        };

        var bySlug = NewsForumDiscussion.FindExistingCategory(
            categories,
            category => category.Name,
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);
        Assert.Equal("NEWS!", bySlug!.Name);

        var byName = NewsForumDiscussion.FindExistingCategory(
            [
                new NamedCategory("The Music"),
                new NamedCategory("News Desk"),
                new NamedCategory("News"),
            ],
            category => category.Name,
            "missing-slug",
            NewsForumDiscussion.CategoryName);
        Assert.Equal("News", byName!.Name);

        Assert.Null(NewsForumDiscussion.FindExistingCategory(
            [new NamedCategory("The Music"), new NamedCategory("News Desk")],
            category => category.Name,
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName));
        Assert.Null(NewsForumDiscussion.FindExistingCategory(
            [new NamedCategory("The Music")],
            category => category.Name,
            "the-music",
            "The Music"));
    }

    private sealed record NamedCategory(string Name);

    [Fact]
    public void TruncatePlain_StripsTagsAndCapsLength()
    {
        var excerpt = NewsForumDiscussion.TruncatePlain(
            "<p>" + new string('x', 500) + "</p>",
            NewsForumDiscussion.OpeningExcerptMaxLength);

        Assert.Equal(NewsForumDiscussion.OpeningExcerptMaxLength, excerpt.Length);
        Assert.DoesNotContain("<p>", excerpt, StringComparison.Ordinal);
    }
}
