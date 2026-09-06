using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryForumRepositoryLegacyDiscographyThreadsTests
{
    [Fact]
    public async Task GetLegacyDiscographyThreadsAsync_ReturnsSeedThreads_OrderedByTitle()
    {
        var repository = new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats());

        var threads = await repository.GetLegacyDiscographyThreadsAsync();

        Assert.NotEmpty(threads);
        Assert.Equal(threads.OrderBy(t => t.Title, StringComparer.Ordinal).Select(t => t.Title), threads.Select(t => t.Title));
        Assert.All(threads, thread => Assert.Equal("Recordings & Rarities", thread.CategoryName));
    }
}
