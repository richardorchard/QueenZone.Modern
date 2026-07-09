using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class BlobContentSnifferTests
{
    [Fact]
    public void Detects_jpeg_png_gif_webp_and_pdf()
    {
        Assert.Equal("image/jpeg", BlobContentSniffer.TryDetectContentType([0xFF, 0xD8, 0xFF, 0xE0]));
        Assert.Equal(
            "image/png",
            BlobContentSniffer.TryDetectContentType([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
        Assert.Equal("image/gif", BlobContentSniffer.TryDetectContentType("GIF89a"u8.ToArray()));
        Assert.Equal(
            "image/webp",
            BlobContentSniffer.TryDetectContentType(
            [
                0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50
            ]));
        Assert.Equal("application/pdf", BlobContentSniffer.TryDetectContentType("%PDF"u8.ToArray()));
        Assert.Null(BlobContentSniffer.TryDetectContentType([0x00, 0x01, 0x02]));
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".JPEG", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".exe", null)]
    public void GuessContentTypeFromExtension(string extension, string? expected) =>
        Assert.Equal(expected, BlobContentSniffer.GuessContentTypeFromExtension(extension));
}
