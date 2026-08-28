using QueenZone.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web.Tests;

public sealed class NewsArticleImageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_center_crops_to_card_aspect_and_makes_webp_derivatives()
    {
        await using var source = await CreatePngAsync(900, 900);

        var result = await NewsArticleImageProcessor.ProcessAsync(source, "square.png");

        await using (result.FullImage)
        await using (result.Thumbnail)
        {
            using var full = await Image.LoadAsync(result.FullImage);
            using var thumb = await Image.LoadAsync(result.Thumbnail);

            Assert.Equal(3 / 2d, full.Width / (double)full.Height, 2);
            Assert.Equal(3 / 2d, thumb.Width / (double)thumb.Height, 2);
            Assert.True(full.Width <= UgcProxyPaths.FullMaxLongestSide);
            Assert.True(thumb.Width <= UgcProxyPaths.ThumbMaxLongestSide);
            Assert.Equal("image/webp", full.Metadata.DecodedImageFormat?.DefaultMimeType);
        }
    }

    [Fact]
    public async Task ProcessAsync_uses_requested_crop_when_valid()
    {
        await using var source = await CreatePngAsync(900, 600);
        var crop = new NewsArticleImageCrop(150, 0, 600, 400);

        var result = await NewsArticleImageProcessor.ProcessAsync(source, "wide.png", crop);

        await using (result.FullImage)
        await using (result.Thumbnail)
        {
            using var full = await Image.LoadAsync(result.FullImage);
            Assert.Equal(3 / 2d, full.Width / (double)full.Height, 2);
        }
    }

    [Fact]
    public void ResolveCrop_ignores_invalid_coordinates_and_center_crops()
    {
        var fallback = NewsArticleImageProcessor.CenterCardCrop(900, 600);
        var ignored = NewsArticleImageProcessor.ResolveCrop(
            900,
            600,
            new NewsArticleImageCrop(0, 0, 100, 100));

        Assert.Equal(fallback, ignored);
        Assert.Equal(900, fallback.Width);
        Assert.Equal(600, fallback.Height);
    }

    [Fact]
    public void CenterCardCrop_crops_portrait_image_to_3_by_2()
    {
        var crop = NewsArticleImageProcessor.CenterCardCrop(600, 900);

        Assert.Equal(0, crop.X);
        Assert.Equal(250, crop.Y);
        Assert.Equal(600, crop.Width);
        Assert.Equal(400, crop.Height);
    }

    [Fact]
    public async Task ProcessAsync_rejects_gif()
    {
        await using var source = await CreateGifAsync(600, 400);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewsArticleImageProcessor.ProcessAsync(source, "card.gif"));

        Assert.Contains("JPEG, PNG, or WebP", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_non_image_bytes()
    {
        await using var source = new MemoryStream("not-an-image"u8.ToArray());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewsArticleImageProcessor.ProcessAsync(source, "note.txt"));

        Assert.Contains("JPEG, PNG, or WebP", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_oversized_payload()
    {
        var bytes = new byte[NewsArticleImageProcessor.MaxUploadBytes + 1];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        await using var source = new MemoryStream(bytes);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewsArticleImageProcessor.ProcessAsync(source, "huge.jpg"));

        Assert.Contains("bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_image_that_is_too_small()
    {
        await using var source = await CreatePngAsync(120, 80);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewsArticleImageProcessor.ProcessAsync(source, "tiny.png"));

        Assert.Contains("too small", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_rejects_extension_mismatch()
    {
        await using var source = await CreatePngAsync(600, 400);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewsArticleImageProcessor.ProcessAsync(source, "photo.jpg"));

        Assert.Contains("extension", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminNewsForm_parses_crop_coordinates()
    {
        var form = new QueenZone.Web.Pages.Admin.News.AdminNewsForm
        {
            CropX = "12",
            CropY = "8",
            CropWidth = "600",
            CropHeight = "400",
        };

        var crop = form.ToCrop();
        Assert.NotNull(crop);
        Assert.Equal(new NewsArticleImageCrop(12, 8, 600, 400), crop);
        Assert.Null(new QueenZone.Web.Pages.Admin.News.AdminNewsForm().ToCrop());
    }

    private static async Task<MemoryStream> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateGifAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new GifEncoder());
        stream.Position = 0;
        return stream;
    }
}
