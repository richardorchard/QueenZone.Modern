using Microsoft.Extensions.Options;
using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

/// <summary>
/// Exercises validation that runs before any storage backend write.
/// </summary>
public sealed class AzureBlobUploadServiceValidationTests
{
    private static AzureBlobUploadService CreateService(BlobUploadOptions? options = null) =>
        new(new InMemoryBlobStorageBackend(), Options.Create(options ?? new BlobUploadOptions()));

    [Fact]
    public async Task UploadAsync_rejects_empty_stream()
    {
        var service = CreateService();
        await using var stream = new MemoryStream();
        var ex = await Assert.ThrowsAsync<BlobUploadException>(() =>
            service.UploadAsync(stream, "empty.jpg", BlobUploadContainers.Avatars));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_rejects_unknown_container()
    {
        var service = CreateService();
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);
        await Assert.ThrowsAsync<BlobUploadException>(() =>
            service.UploadAsync(stream, "x.jpg", "not-a-container"));
    }

    [Fact]
    public async Task UploadAsync_rejects_oversized_payload()
    {
        var options = new BlobUploadOptions
        {
            Containers =
            {
                [BlobUploadContainers.Avatars] = new BlobContainerPolicy
                {
                    MaxBytes = 8,
                    AllowedContentTypes = ["image/jpeg"],
                },
            },
        };
        var service = CreateService(options);

        // 9 bytes of JPEG-like header padding
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5]);
        var ex = await Assert.ThrowsAsync<BlobUploadException>(() =>
            service.UploadAsync(stream, "big.jpg", BlobUploadContainers.Avatars));
        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BlobUploadException_supports_inner_exception()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new BlobUploadException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
