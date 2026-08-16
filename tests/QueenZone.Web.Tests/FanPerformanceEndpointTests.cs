using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceEndpointTests
{
    [Fact]
    public async Task ServeAudioAsync_ReturnsNotFound_WhenPerformanceMissing()
    {
        var repo = new InMemoryFanPerformanceRepository([]);
        var result = await FanPerformanceEndpoints.ServeAudioAsync(
            999,
            repo,
            new MemoryBlobUploadService(),
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task ServeAudioAsync_ReturnsNotFound_WhenFilenameUnsafe()
    {
        var repo = new InMemoryFanPerformanceRepository(
        [
            new(1, "Secret", "X", "", "../secret.mp3", 10, DateTime.UtcNow),
        ]);

        var result = await FanPerformanceEndpoints.ServeAudioAsync(
            1,
            repo,
            new MemoryBlobUploadService(),
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task ServeAudioAsync_ReturnsNotFound_WhenBlobMissing()
    {
        var repo = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());

        var result = await FanPerformanceEndpoints.ServeAudioAsync(
            187,
            repo,
            new MemoryBlobUploadService(),
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task ServeAudioAsync_ReturnsNotFound_WhenStorageNotConfigured()
    {
        var repo = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());

        var result = await FanPerformanceEndpoints.ServeAudioAsync(
            187,
            repo,
            new NullBlobUploadService(),
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task ServeAudioAsync_StreamsBlob_WhenPresent()
    {
        var repo = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());
        var blobs = new MemoryBlobUploadService();
        await using var payload = new MemoryStream(Encoding.ASCII.GetBytes("ID3fake"));
        await blobs.UploadAsync(
            payload,
            "2014417798057369.mp3",
            SongFileUrl.ContainerName,
            new BlobUploadContext { PreferredBlobName = "2014417798057369.mp3" });

        var result = await FanPerformanceEndpoints.ServeAudioAsync(
            187,
            repo,
            blobs,
            CancellationToken.None);

        var file = Assert.IsType<FileStreamHttpResult>(result);
        Assert.Equal("audio/mpeg", file.ContentType);
        Assert.Equal("Reaching-Out.mp3", file.FileDownloadName);
        Assert.True(file.EnableRangeProcessing);

        using var reader = new StreamReader(file.FileStream!);
        Assert.Equal("ID3fake", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task ServeAudioAsync_FallsBackToOctetStream_WhenBlobHasNoContentType()
    {
        var repo = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());
        var blobs = new EmptyContentTypeBlobUploadService();

        var result = await FanPerformanceEndpoints.ServeAudioAsync(
            187,
            repo,
            blobs,
            CancellationToken.None);

        var file = Assert.IsType<FileStreamHttpResult>(result);
        Assert.Equal(MediaTypeNames.Application.Octet, file.ContentType);
    }

    private sealed class EmptyContentTypeBlobUploadService : IBlobUploadService
    {
        public Task<BlobUploadResult> UploadAsync(
            Stream content,
            string originalFileName,
            string containerName,
            BlobUploadContext? context = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<BlobContent?> OpenReadAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BlobContent?>(new BlobContent
            {
                Stream = new MemoryStream([1, 2, 3]),
                ContentType = string.Empty,
            });
    }
}
