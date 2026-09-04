using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceAudioUploadServiceTests
{
    [Fact]
    public async Task UploadPendingAsync_writes_to_pending_container_not_songfiles()
    {
        var service = CreateService(out var backend, out _);
        var memberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await using var stream = new MemoryStream(CreateMpegPayload(16_000));

        var result = await service.UploadPendingAsync(memberId, stream, "cover.mp3");

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Blob);
        Assert.Equal(BlobUploadContainers.FanPerformances, result.Blob.Container);
        Assert.NotEqual(SongFileUrl.ContainerName, result.Blob.Container);
        Assert.StartsWith($"members/{memberId:N}/", result.Blob.BlobName);
        Assert.EndsWith(".mp3", result.Blob.BlobName);
        Assert.Equal("audio/mpeg", result.Blob.ContentType);
        Assert.Equal(1, result.DurationSeconds);
        Assert.True(backend.Exists(result.Blob.Container, result.Blob.BlobName));
        Assert.False(backend.Exists(SongFileUrl.ContainerName, result.Blob.BlobName));
    }

    [Fact]
    public async Task UploadPendingAsync_accepts_flac_and_returns_streaminfo_duration()
    {
        var service = CreateService(out _, out _);
        var prefix = AudioDurationTests.CreateFlacStreamInfo(44_100, 44_100);
        await using var stream = new MemoryStream(prefix);

        var result = await service.UploadPendingAsync(Guid.NewGuid(), stream, "cover.flac");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("audio/flac", result.Blob!.ContentType);
        Assert.Equal(BlobUploadContainers.FanPerformances, result.Blob.Container);
        Assert.Equal(1, result.DurationSeconds);
    }

    [Fact]
    public async Task UploadPendingAsync_rejects_fake_mp3_without_consuming_quota()
    {
        var quota = CreateQuota(maxUploads: 1, maxBytes: 1_000_000);
        var backend = new InMemoryBlobStorageBackend();
        var service = new FanPerformanceAudioUploadService(
            new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions())),
            quota,
            Options.Create(new BlobUploadOptions()));

        var memberId = Guid.NewGuid();
        await using var fake = new MemoryStream("not-an-mp3"u8.ToArray());
        var rejected = await service.UploadPendingAsync(memberId, fake, "fake.mp3");
        Assert.False(rejected.Succeeded);
        Assert.Contains("not recognized as audio", rejected.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(rejected.Blob);

        await using var real = new MemoryStream(CreateMpegPayload(200));
        var accepted = await service.UploadPendingAsync(memberId, real, "real.mp3");
        Assert.True(accepted.Succeeded, accepted.Error);
    }

    [Fact]
    public async Task UploadPendingAsync_rejects_over_ceiling_with_clear_message()
    {
        var options = new BlobUploadOptions
        {
            Containers =
            {
                [BlobUploadContainers.FanPerformances] = new BlobContainerPolicy
                {
                    MaxBytes = 50,
                    AllowedContentTypes = ["audio/mpeg", "audio/mp3", "audio/flac", "audio/x-flac"],
                },
            },
        };
        var backend = new InMemoryBlobStorageBackend();
        var service = new FanPerformanceAudioUploadService(
            new AzureBlobUploadService(backend, Options.Create(options)),
            CreateQuota(maxUploads: 10, maxBytes: 1_000_000),
            Options.Create(options));

        await using var stream = new MemoryStream(CreateMpegPayload(80));
        var result = await service.UploadPendingAsync(Guid.NewGuid(), stream, "big.mp3");

        Assert.False(result.Succeeded);
        Assert.Contains("exceeds", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("50", result.Error);
        Assert.Null(result.Blob);
    }

    [Fact]
    public async Task UploadPendingAsync_uses_shared_member_quota_before_write()
    {
        var quota = CreateQuota(maxUploads: 1, maxBytes: 1_000_000);
        var backend = new InMemoryBlobStorageBackend();
        var audio = new FanPerformanceAudioUploadService(
            new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions())),
            quota,
            Options.Create(new BlobUploadOptions()));
        var memberId = Guid.NewGuid();
        var principalKey = MemberUploadQuotaService.PrincipalKeyFromMemberId(memberId);

        Assert.True(quota.TryConsume(principalKey, 10, out _));

        await using var stream = new MemoryStream(CreateMpegPayload(200));
        var blocked = await audio.UploadPendingAsync(memberId, stream, "two.mp3");
        Assert.False(blocked.Succeeded);
        Assert.Contains("Daily upload", blocked.Error);
        Assert.Null(blocked.Blob);
    }

    [Fact]
    public async Task UploadPendingAsync_accepts_upload_when_duration_is_unknown()
    {
        var service = CreateService(out var backend, out _);
        var id3Only = "ID3\0\0\0\0\0\0\0\0junk"u8.ToArray();
        await using var stream = new MemoryStream(id3Only);

        var result = await service.UploadPendingAsync(Guid.NewGuid(), stream, "tagged.mp3");

        Assert.True(result.Succeeded, result.Error);
        Assert.Null(result.DurationSeconds);
        Assert.True(backend.Exists(result.Blob!.Container, result.Blob.BlobName));
    }

    [Fact]
    public async Task UploadPendingAsync_rejects_empty_member_and_file_name()
    {
        var service = CreateService(out _, out _);
        await using var stream = new MemoryStream(CreateMpegPayload(64));

        var unsigned = await service.UploadPendingAsync(Guid.Empty, stream, "cover.mp3");
        Assert.False(unsigned.Succeeded);
        Assert.Contains("Sign in", unsigned.Error);

        stream.Position = 0;
        var missingName = await service.UploadPendingAsync(Guid.NewGuid(), stream, "  ");
        Assert.False(missingName.Succeeded);
        Assert.Contains("file name", missingName.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static FanPerformanceAudioUploadService CreateService(
        out InMemoryBlobStorageBackend backend,
        out MemberUploadQuotaService quota)
    {
        backend = new InMemoryBlobStorageBackend();
        quota = CreateQuota(maxUploads: 50, maxBytes: 100L * 1024 * 1024);
        return new FanPerformanceAudioUploadService(
            new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions())),
            quota,
            Options.Create(new BlobUploadOptions()));
    }

    private static MemberUploadQuotaService CreateQuota(int maxUploads, long maxBytes) =>
        new(
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            Options.Create(new UploadQuotaOptions
            {
                Enabled = true,
                MaxUploadsPerDay = maxUploads,
                MaxBytesPerDay = maxBytes,
            }));

    private static byte[] CreateMpegPayload(int length)
    {
        var bytes = new byte[Math.Max(length, 4)];
        Mp3DurationTests.CreateMpeg1Layer3Header(9).CopyTo(bytes.AsSpan());
        return bytes;
    }
}
