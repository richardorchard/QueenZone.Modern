using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class InMemoryBlobStorageBackendTests
{
    [Fact]
    public async Task OpenReadAsync_exposes_opaque_etag_and_content_length()
    {
        var backend = new InMemoryBlobStorageBackend();
        var payload = "ID3fake-audio"u8.ToArray();
        await using var upload = new MemoryStream(payload);
        await backend.UploadAsync("songfiles", "clip.mp3", upload, "audio/mpeg");

        await using var content = await backend.OpenReadAsync("songfiles", "clip.mp3");

        Assert.NotNull(content);
        Assert.Equal("audio/mpeg", content!.ContentType);
        Assert.Equal(payload.LongLength, content.ContentLength);
        Assert.Equal(InMemoryBlobStorageBackend.ComputeETag(payload), content.ETag);
        Assert.StartsWith("\"", content.ETag);
        Assert.EndsWith("\"", content.ETag);
        Assert.DoesNotContain("songfiles", content.ETag, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clip.mp3", content.ETag, StringComparison.OrdinalIgnoreCase);
    }
}
