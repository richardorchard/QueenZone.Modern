using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryPhotoRepositoryRandomSampleTests
{
    [Fact]
    public async Task GetRandomPublishedInCategoryAsync_returns_up_to_take_without_requiring_full_list_consumer()
    {
        var repository = new InMemoryPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));

        var sample = await repository.GetRandomPublishedInCategoryAsync(18, take: 4);

        Assert.Equal(4, sample.Count);
        Assert.All(sample, photo => Assert.Equal(18, photo.CatId));
        Assert.Equal(4, sample.Select(photo => photo.PicId).Distinct().Count());
    }

    [Fact]
    public async Task GetRandomPublishedInCategoryAsync_returns_empty_for_missing_or_empty_category()
    {
        var repository = new InMemoryPhotoRepository(new SharedPhotoStore(
        [
            new PhotoCategorySeed(9, "Brian May", []),
        ]));

        Assert.Empty(await repository.GetRandomPublishedInCategoryAsync(9, take: 4));
        Assert.Empty(await repository.GetRandomPublishedInCategoryAsync(99, take: 4));
    }
}
