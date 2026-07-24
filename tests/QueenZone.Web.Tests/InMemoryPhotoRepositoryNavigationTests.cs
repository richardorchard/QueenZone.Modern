using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryPhotoRepositoryNavigationTests
{
    [Fact]
    public async Task GetDetailNavigation_ReturnsNeighborsWithoutRequiringFullClientScanApi()
    {
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var repository = new InMemoryPhotoRepository(store);

        var categories = await repository.GetCategoriesAsync();
        Assert.All(categories, category => Assert.False(string.IsNullOrWhiteSpace(category.CoverThumbnailUrl)));

        var brian = categories.Single(category => category.Slug == "brian-may");
        var page = await repository.GetCategoryPageAsync(brian.CatId, 1, 2);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.TotalCount >= 2);

        var first = page.Items[0];
        var navigation = await repository.GetDetailNavigationAsync(brian.CatId, first.PicId);
        Assert.NotNull(navigation);
        Assert.Equal(0, navigation.Index);
        Assert.Null(navigation.PreviousPicId);
        Assert.NotNull(navigation.NextPicId);
    }
}
