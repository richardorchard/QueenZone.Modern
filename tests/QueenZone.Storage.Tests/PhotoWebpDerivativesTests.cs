using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class PhotoWebpDerivativesTests
{
    [Fact]
    public async Task CreateSquareThumbnailAsync_produces_webp_square()
    {
        using var image = new Image<Rgba32>(800, 400, new Rgba32(10, 20, 30));

        await using var encoded = await PhotoWebpDerivatives.CreateSquareThumbnailAsync(image);

        using var loaded = await Image.LoadAsync(encoded.Stream);
        Assert.Equal(PhotoWebpDerivatives.DefaultThumbSizePixels, loaded.Width);
        Assert.Equal(PhotoWebpDerivatives.DefaultThumbSizePixels, loaded.Height);
        Assert.Equal(PhotoWebpDerivatives.DefaultThumbSizePixels, encoded.WidthPx);
        Assert.Equal(PhotoWebpDerivatives.DefaultThumbSizePixels, encoded.HeightPx);
    }

    [Fact]
    public async Task CreateMaxSideAsync_caps_longest_side()
    {
        using var image = new Image<Rgba32>(2400, 1200, new Rgba32(40, 50, 60));

        await using var encoded = await PhotoWebpDerivatives.CreateMaxSideAsync(image, maxLongestSide: 1000);

        using var loaded = await Image.LoadAsync(encoded.Stream);
        Assert.True(loaded.Width <= 1000);
        Assert.True(loaded.Height <= 1000);
        Assert.Equal(1000, loaded.Width);
        Assert.Equal(500, loaded.Height);
    }

    [Theory]
    [InlineData("celeb2.jpg", "celeb2_t.webp")]
    [InlineData("folder/Diapositiva6.JPG", "Diapositiva6_t.webp")]
    [InlineData("noext", "noext_t.webp")]
    public void ToThumbnailBlobName_uses_stem_and_webp(string input, string expected) =>
        Assert.Equal(expected, PhotoWebpDerivatives.ToThumbnailBlobName(input));

    [Fact]
    public void ToLegacyThumbnailPath_keeps_folder_segment()
    {
        var path = PhotoWebpDerivatives.ToLegacyThumbnailPath(
            "/Freddie_Mercury/celeb2.jpg",
            "celeb2_t.webp");

        Assert.Equal("/Freddie_Mercury/celeb2_t.webp", path);
    }
}
