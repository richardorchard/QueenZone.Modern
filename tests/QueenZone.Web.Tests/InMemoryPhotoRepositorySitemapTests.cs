using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryPhotoRepositorySitemapTests
{
    [Fact]
    public async Task GetPublishedSitemapCategoriesAsync_returns_only_non_empty_categories_with_light_photos()
    {
        var repository = new InMemoryPhotoRepository(SamplePhotoData.CreateSeedCategories());

        var categories = await repository.GetPublishedSitemapCategoriesAsync();

        Assert.Equal(2, categories.Count);
        Assert.Contains(categories, category => category.Slug == "brian-may" && category.Photos.Count == 3);
        Assert.Contains(categories, category => category.Slug == "queen" && category.Photos.Any(photo => photo.PicId == 201));
        Assert.All(categories, category =>
        {
            Assert.NotEmpty(category.Photos);
            Assert.All(category.Photos, photo => Assert.True(photo.PicId > 0));
        });
    }
}
