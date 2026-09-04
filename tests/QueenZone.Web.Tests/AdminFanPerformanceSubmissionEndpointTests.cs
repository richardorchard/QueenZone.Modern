using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminFanPerformanceSubmissionEndpointTests
{
    [Fact]
    public async Task ServePendingAudioAsync_ReturnsNotFound_WhenSubmissionMissing()
    {
        var result = await AdminFanPerformanceSubmissionEndpoints.ServePendingAudioAsync(
            Guid.NewGuid(),
            new InMemoryFanPerformanceSubmissionRepository(),
            new MemoryBlobUploadService(),
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task ServePendingAudioAsync_ReturnsNotFound_WhenBlobPathEmpty()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission("pending/cover.mp3"));
        await repository.ClearPendingBlobPathAsync(created.Id);

        var result = await AdminFanPerformanceSubmissionEndpoints.ServePendingAudioAsync(
            created.Id,
            repository,
            new MemoryBlobUploadService(),
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task ServePendingAudioAsync_StreamsPendingBlob()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission("pending/cover.mp3"));
        var blobs = new MemoryBlobUploadService();
        await using var payload = new MemoryStream(Encoding.ASCII.GetBytes("ID3fake"));
        await blobs.UploadAsync(
            payload,
            "cover.mp3",
            BlobUploadContainers.FanPerformances,
            new BlobUploadContext { PreferredBlobName = created.BlobPath });

        var result = await AdminFanPerformanceSubmissionEndpoints.ServePendingAudioAsync(
            created.Id,
            repository,
            blobs,
            CancellationToken.None);

        var file = Assert.IsType<FileStreamHttpResult>(result);
        Assert.Equal("audio/mpeg", file.ContentType);
        Assert.True(file.EnableRangeProcessing);
        using var reader = new StreamReader(file.FileStream!);
        Assert.Equal("ID3fake", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task ServePendingAudioAsync_FallsBackToOctetStream_WhenBlobHasNoContentType()
    {
        var repository = new InMemoryFanPerformanceSubmissionRepository();
        var created = await repository.CreateAsync(NewSubmission("pending/cover.mp3"));

        var result = await AdminFanPerformanceSubmissionEndpoints.ServePendingAudioAsync(
            created.Id,
            repository,
            new EmptyContentTypeBlobUploadService(),
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

    private static NewFanPerformanceSubmission NewSubmission(string blobPath) =>
        new(
            Guid.NewGuid(),
            "Title",
            "Song",
            "Fan",
            null,
            blobPath,
            "cover.mp3",
            10,
            "audio/mpeg",
            1,
            DateTimeOffset.UtcNow,
            FanPerformanceSubmissionRights.DeclarationVersion);
}
