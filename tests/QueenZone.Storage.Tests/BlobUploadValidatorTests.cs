using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class BlobUploadValidatorTests
{
    private readonly BlobUploadValidator validator = new(new BlobUploadOptions());

    [Fact]
    public void Rejects_unknown_container()
    {
        var ex = Assert.Throws<BlobUploadException>(() => validator.EnsureKnownContainer("legacy-photos"));
        Assert.Contains("not a known UGC container", ex.Message);
    }

    [Fact]
    public void Accepts_canonical_containers()
    {
        foreach (var container in BlobUploadContainers.All)
        {
            validator.EnsureKnownContainer(container);
        }
    }

    [Fact]
    public void Rejects_empty_and_oversized_payloads()
    {
        Assert.Throws<BlobUploadException>(() =>
            validator.ValidateSize(0, BlobUploadContainers.Avatars));

        var max = validator.GetMaxBytes(BlobUploadContainers.Avatars);
        Assert.Throws<BlobUploadException>(() =>
            validator.ValidateSize(max + 1, BlobUploadContainers.Avatars));

        validator.ValidateSize(max, BlobUploadContainers.Avatars);
    }

    [Fact]
    public void Avatar_limit_is_stricter_than_default()
    {
        Assert.True(
            validator.GetMaxBytes(BlobUploadContainers.Avatars)
            < validator.GetMaxBytes(BlobUploadContainers.Forum));
    }

    [Fact]
    public void Rejects_disallowed_content_type_for_container()
    {
        // PDF allowed for forum, not avatars
        var pdfHeader = "%PDF-1.4"u8.ToArray();
        Assert.Throws<BlobUploadException>(() =>
            validator.ResolveAndValidateContentType("doc.pdf", pdfHeader, BlobUploadContainers.Avatars));

        var contentType = validator.ResolveAndValidateContentType(
            "doc.pdf",
            pdfHeader,
            BlobUploadContainers.Forum);
        Assert.Equal("application/pdf", contentType);
    }

    [Fact]
    public void Rejects_extension_content_mismatch()
    {
        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var ex = Assert.Throws<BlobUploadException>(() =>
            validator.ResolveAndValidateContentType(
                "not-a-png.png",
                jpegHeader,
                BlobUploadContainers.Photos));
        Assert.Contains("does not match extension", ex.Message);
    }

    [Fact]
    public void Accepts_matching_jpeg()
    {
        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var contentType = validator.ResolveAndValidateContentType(
            "photo.jpg",
            jpegHeader,
            BlobUploadContainers.Articles);
        Assert.Equal("image/jpeg", contentType);
    }
}
