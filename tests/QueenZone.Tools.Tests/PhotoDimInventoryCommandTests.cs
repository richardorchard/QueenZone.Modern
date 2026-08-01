using QueenZone.Data;
using QueenZone.Tools;

namespace QueenZone.Tools.Tests;

public sealed class PhotoDimInventoryCommandTests
{
    [Fact]
    public void Parse_RequiresConnectionString()
    {
        var options = PhotoDimInventoryOptions.Parse([]);
        Assert.False(options.IsValid);
        Assert.Contains("connection", options.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_AcceptsFilters()
    {
        var options = PhotoDimInventoryOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
            "--category-slug", "brian-may",
            "--limit", "5",
            "--output", "report.txt",
        ]);

        Assert.True(options.IsValid);
        Assert.Equal("brian-may", options.CategorySlug);
        Assert.Equal(5, options.Limit);
        Assert.Equal("report.txt", options.OutputPath);
    }

    [Fact]
    public async Task LoadPublicPhotosAsync_UsesRepositoryAndLimit()
    {
        var repository = new InMemoryPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));
        var options = PhotoDimInventoryOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
            "--category-slug", "brian-may",
            "--limit", "2",
        ]);

        var photos = await PhotoDimInventoryCommand.LoadPublicPhotosAsync(options, repository);
        Assert.Equal(2, photos.Count);
        Assert.All(photos, photo => Assert.Equal("brian-may", photo.CategorySlug));
    }
}
