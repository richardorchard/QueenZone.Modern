using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class NullBlobUploadServiceTests
{
    private readonly NullBlobUploadService service = new();

    [Fact]
    public async Task UploadAsync_throws_not_supported()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            service.UploadAsync(stream, "a.jpg", BlobUploadContainers.Avatars));
        Assert.Contains("Blob storage is not configured", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_throws_not_supported()
    {
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            service.DeleteAsync(BlobUploadContainers.Avatars, "members/1/x.jpg"));
        Assert.Contains("Blob storage is not configured", ex.Message);
    }
}
