using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumWriteAuthorMatchingTests
{
    [Fact]
    public void MatchesPost_UsesMemberIdOrUnlinkedExactName()
    {
        var memberId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        Assert.True(ForumAuthorContentMatching.MatchesPost(memberId, "Spammer", memberId, "Other"));
        Assert.True(ForumAuthorContentMatching.MatchesPost(memberId, "Spammer", null, "spammer"));
        Assert.False(ForumAuthorContentMatching.MatchesPost(memberId, "Spammer", otherId, "Spammer"));
        Assert.False(ForumAuthorContentMatching.MatchesPost(null, "Spammer", otherId, "Spammer"));
        Assert.True(ForumAuthorContentMatching.MatchesPost(null, " PatriciaCMardis ", null, "patriciacmardis"));
        Assert.False(ForumAuthorContentMatching.MatchesPost(null, "Pat", null, "PatriciaCMardis"));
    }

    [Fact]
    public void MatchesStartedThread_UsesFirstPostOrUnlinkedStartedByName()
    {
        var memberId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        Assert.True(ForumAuthorContentMatching.MatchesStartedThread(
            memberId, "Spammer", memberId, "Spammer", "Someone Else"));
        Assert.True(ForumAuthorContentMatching.MatchesStartedThread(
            null, "PatriciaCMardis", null, "Other", "patriciacmardis"));
        Assert.False(ForumAuthorContentMatching.MatchesStartedThread(
            null, "PatriciaCMardis", otherId, "Other", "PatriciaCMardis"));
    }
}
