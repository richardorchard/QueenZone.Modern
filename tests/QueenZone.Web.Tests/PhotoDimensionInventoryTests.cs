using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PhotoDimensionInventoryTests
{
    [Fact]
    public void FromDimensions_AggregatesCoverageWallpaperAndBuckets()
    {
        var report = PhotoDimensionInventory.FromDimensions(
        [
            (1920, 1080), // desktop + large + landscape
            (1080, 1920), // phone + large + portrait
            (1024, 1024), // square mid
            (0, 0),       // missing
            (800, 0),     // missing (partial)
            (640, 480),   // small landscape
            (2560, 1440), // desktop + large
        ]);

        Assert.Equal(7, report.TotalPublic);
        Assert.Equal(5, report.UsableDimensions);
        Assert.Equal(2, report.MissingOrZeroDimensions);
        Assert.Equal(2, report.DesktopWallpaperCandidates);
        Assert.Equal(1, report.PhoneWallpaperCandidates);
        Assert.Equal(3, report.LargeCandidates);
        Assert.Equal(3, report.LandscapeUsable);
        Assert.Equal(1, report.PortraitUsable);
        Assert.Equal(1, report.SquareUsable);

        Assert.Equal(2, report.LongestSideBuckets.Single(b => b.Label.StartsWith("0", StringComparison.Ordinal)).Count);
        Assert.Equal(1, report.LongestSideBuckets.Single(b => b.Label == "1–799").Count);
        Assert.Equal(1, report.LongestSideBuckets.Single(b => b.Label == "800–1279").Count);
        Assert.Equal(2, report.LongestSideBuckets.Single(b => b.Label == "1920–2559").Count);
        Assert.Equal(1, report.LongestSideBuckets.Single(b => b.Label == "2560+").Count);

        var text = PhotoDimensionInventory.FormatText(report);
        Assert.Contains("Desktop", text, StringComparison.Ordinal);
        Assert.Contains("Recommendation:", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FromPhotos_UsesSampleSeedOriginalDimensions()
    {
        var repository = new InMemoryPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));
        var categories = await repository.GetCategoriesAsync();
        var photos = new List<PhotoItem>();
        foreach (var category in categories)
        {
            photos.AddRange(await repository.GetCategoryAllAsync(category.CatId));
        }

        var report = PhotoDimensionInventory.FromPhotos(photos);
        Assert.Equal(11, report.TotalPublic);
        Assert.Equal(10, report.UsableDimensions);
        Assert.Equal(1, report.MissingOrZeroDimensions);
        Assert.True(report.DesktopWallpaperCandidates >= 1);
        Assert.True(report.PhoneWallpaperCandidates >= 1);
    }

    [Fact]
    public void PhotoItem_HidesZeroDimensionsLabel()
    {
        var known = new PhotoItem(1, 1, "C", "c", "T", "u", "t", 10, 10, 1920, 1080, 1986, DateTime.UnixEpoch);
        Assert.True(known.HasPictureDimensions);
        Assert.Equal("1920 x 1080", known.PictureDimensionsLabel);

        var unknown = known with { PictureWidth = 0, PictureHeight = 0 };
        Assert.False(unknown.HasPictureDimensions);
        Assert.Null(unknown.PictureDimensionsLabel);
    }
}
