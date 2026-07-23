using Microsoft.Extensions.Options;
using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class AzureBlobUploadServiceHappyPathTests
{
    [Fact]
    public async Task UploadAsync_stores_blob_and_returns_result_with_cdn_public_url()
    {
        var backend = new InMemoryBlobStorageBackend();
        var options = Options.Create(new BlobUploadOptions
        {
            PublicBaseUrl = "https://cdn.example.test/ugc",
        });
        var service = new AzureBlobUploadService(backend, options);

        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0xFF, 0xD9]);
        var result = await service.UploadAsync(
            stream,
            "portrait.JPG",
            BlobUploadContainers.Avatars,
            new BlobUploadContext { MemberId = 7 });

        Assert.Equal(BlobUploadContainers.Avatars, result.Container);
        Assert.StartsWith("members/7/", result.BlobName);
        Assert.EndsWith(".jpg", result.BlobName);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal(8, result.SizeBytes);
        Assert.Equal(
            $"https://cdn.example.test/ugc/{result.Container}/{result.BlobName}",
            result.PublicUrl);

        Assert.True(backend.Exists(result.Container, result.BlobName));
        var stored = backend.TryGet(result.Container, result.BlobName);
        Assert.NotNull(stored);
        Assert.Equal("image/jpeg", stored.Value.ContentType);
    }

    [Fact]
    public async Task UploadAsync_uses_storage_uri_when_public_base_url_missing()
    {
        var backend = new InMemoryBlobStorageBackend();
        var service = new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions()));

        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0xFF, 0xD9]);
        var result = await service.UploadAsync(stream, "x.jpg", BlobUploadContainers.Articles);

        Assert.StartsWith("anonymous/", result.BlobName);
        Assert.StartsWith("https://memory.blob.test/", result.PublicUrl);
    }

    [Fact]
    public async Task UploadAsync_rejects_missing_file_name()
    {
        var service = new AzureBlobUploadService(
            new InMemoryBlobStorageBackend(),
            Options.Create(new BlobUploadOptions()));
        await using var stream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<BlobUploadException>(() =>
            service.UploadAsync(stream, "  ", BlobUploadContainers.Forum));
    }

    [Fact]
    public async Task DeleteAsync_removes_blob_and_rejects_blank_keys()
    {
        var backend = new InMemoryBlobStorageBackend();
        var service = new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions()));

        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0xFF, 0xD9]);
        var result = await service.UploadAsync(stream, "x.jpg", BlobUploadContainers.Photos);
        Assert.True(backend.Exists(result.Container, result.BlobName));

        await service.DeleteAsync(result.Container, result.BlobName);
        Assert.False(backend.Exists(result.Container, result.BlobName));

        await Assert.ThrowsAsync<BlobUploadException>(() =>
            service.DeleteAsync(" ", "name"));
        await Assert.ThrowsAsync<BlobUploadException>(() =>
            service.DeleteAsync(BlobUploadContainers.Photos, " "));
    }

    [Fact]
    public async Task UploadAsync_editor_context_and_extensionless_name()
    {
        var backend = new InMemoryBlobStorageBackend();
        var service = new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions()));

        // Extensionless file still allowed if sniff detects JPEG
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0xFF, 0xD9]);
        var result = await service.UploadAsync(
            stream,
            "no-extension",
            BlobUploadContainers.Articles,
            new BlobUploadContext { ActorEmail = "!!!bad@@" });

        Assert.StartsWith("editors/", result.BlobName);
        // Sanitized email collapses to something non-empty or "unknown"
        Assert.DoesNotContain("!", result.BlobName);
    }

    [Fact]
    public async Task BufferForUploadAsync_non_seekable_stream_uses_temp_file_and_enforces_max()
    {
        var payload = new byte[32];
        Random.Shared.NextBytes(payload);
        await using var nonSeekable = new NonSeekableStream(payload);

        await using var buffered = await AzureBlobUploadService.BufferForUploadAsync(
            nonSeekable,
            maxBytes: 1024,
            CancellationToken.None);

        Assert.True(buffered.CanSeek);
        Assert.Equal(payload.Length, buffered.Length);
        var copy = new byte[payload.Length];
        Assert.Equal(payload.Length, await buffered.ReadAsync(copy));
        Assert.Equal(payload, copy);
    }

    [Fact]
    public async Task BufferForUploadAsync_rejects_oversize_non_seekable_stream()
    {
        var payload = new byte[100];
        await using var nonSeekable = new NonSeekableStream(payload);

        var ex = await Assert.ThrowsAsync<BlobUploadException>(() =>
            AzureBlobUploadService.BufferForUploadAsync(nonSeekable, maxBytes: 50, CancellationToken.None));
        Assert.Contains("50", ex.Message, StringComparison.Ordinal);
    }

    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private int position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position >= data.Length)
            {
                return 0;
            }

            var n = Math.Min(count, data.Length - position);
            Array.Copy(data, position, buffer, offset, n);
            position += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
