using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class EditorImageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_resizes_oversized_image_and_produces_thumb()
    {
        await using var source = await CreatePngAsync(1800, 900);

        var result = await EditorImageProcessor.ProcessAsync(source, "big.png");

        await using (result.FullImage)
        await using (result.Thumbnail)
        {
            using var full = await Image.LoadAsync(result.FullImage);
            using var thumb = await Image.LoadAsync(result.Thumbnail);

            Assert.True(full.Width <= UgcProxyPaths.FullMaxLongestSide);
            Assert.True(full.Height <= UgcProxyPaths.FullMaxLongestSide);
            Assert.Equal(1200, full.Width); // 1800x900 -> 1200x600
            Assert.Equal(600, full.Height);

            Assert.True(thumb.Width <= UgcProxyPaths.ThumbMaxLongestSide);
            Assert.True(thumb.Height <= UgcProxyPaths.ThumbMaxLongestSide);
            Assert.Equal(600, thumb.Width);
            Assert.Equal(300, thumb.Height);
        }
    }

    [Fact]
    public async Task ProcessAsync_rejects_non_image_bytes()
    {
        await using var source = new MemoryStream("not-an-image"u8.ToArray());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EditorImageProcessor.ProcessAsync(source, "note.txt"));

        Assert.Contains("image", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_oversized_payload()
    {
        var bytes = new byte[EditorImageUploadEndpoints.MaxImageBytes + 1];
        // Minimal JPEG SOI so size check is hit after copy.
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        await using var source = new MemoryStream(bytes);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EditorImageProcessor.ProcessAsync(source, "huge.jpg"));

        Assert.Contains("bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_empty_stream()
    {
        await using var source = new MemoryStream();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EditorImageProcessor.ProcessAsync(source, "empty.png"));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_extension_mismatch()
    {
        await using var source = await CreatePngAsync(40, 40);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EditorImageProcessor.ProcessAsync(source, "photo.jpg"));
        Assert.Contains("extension", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_leaves_small_images_unscaled()
    {
        await using var source = await CreatePngAsync(100, 80);
        var result = await EditorImageProcessor.ProcessAsync(source, "small.png");
        await using (result.FullImage)
        await using (result.Thumbnail)
        {
            using var full = await Image.LoadAsync(result.FullImage);
            Assert.Equal(100, full.Width);
            Assert.Equal(80, full.Height);
            Assert.Equal("small.webp", result.FullFileName);
            Assert.Equal("small-thumb.webp", result.ThumbFileName);
        }
    }

    private static async Task<MemoryStream> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }
}
