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
    public void TruncatePlain_StripsTagsAndCapsLength()
    {
        var excerpt = NewsForumDiscussion.TruncatePlain(
            "<p>" + new string('x', 500) + "</p>",
            NewsForumDiscussion.OpeningExcerptMaxLength);

        Assert.Equal(NewsForumDiscussion.OpeningExcerptMaxLength, excerpt.Length);
        Assert.DoesNotContain("<p>", excerpt, StringComparison.Ordinal);
    }
}
