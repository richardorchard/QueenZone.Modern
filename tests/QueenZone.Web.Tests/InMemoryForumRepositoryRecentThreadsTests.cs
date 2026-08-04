using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryForumRepositoryRecentThreadsTests
{
    [Fact]
    public async Task GetRecentThreadsAsync_ReturnsCrossBoardThreadsNewestFirst()
    {
        var repository = new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats());

        var recent = await repository.GetRecentThreadsAsync(5);

        Assert.Equal(5, recent.Count);
        Assert.Equal(1001, recent[0].TopicId);
        Assert.Equal("Forum Guidelines", recent[0].Title);
        Assert.Equal("The Music", recent[0].CategoryName);
        Assert.True(recent[0].LastActivityAt >= recent[^1].LastActivityAt);
    }
}
