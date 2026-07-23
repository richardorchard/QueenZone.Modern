using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class UgcProxyEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public UgcProxyEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task ServeAsync_returns_not_found_for_unknown_area()
    {
        var result = await UgcProxyEndpoints.ServeAsync(
            "unknown",
            "a.webp",
            size: null,
            new StubBlobUploadService(),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task ServeAsync_returns_stream_for_existing_blob()
    {
        var stub = new StubBlobUploadService
        {
            Content = new BlobContent
            {
                Stream = new MemoryStream([1, 2, 3]),
                ContentType = "image/webp",
            },
        };

        var result = await UgcProxyEndpoints.ServeAsync(
            "forum",
            "editors/x.webp",
            size: null,
            stub,
            CancellationToken.None);

        var httpContext = new DefaultHttpContext { RequestServices = factory.Services };
        await result.ExecuteAsync(httpContext);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(UgcProxyEndpoints.CacheControlHeaderValue, httpContext.Response.Headers.CacheControl.ToString());
        Assert.Equal(BlobUploadContainers.Forum, stub.LastContainer);
        Assert.Equal("editors/x.webp", stub.LastBlobName);
    }

    [Fact]
    public async Task ServeAsync_requests_thumb_blob_when_size_thumb()
    {
        var stub = new StubBlobUploadService
        {
            Content = new BlobContent
            {
                Stream = new MemoryStream([9]),
                ContentType = "image/webp",
            },
        };

        var result = await UgcProxyEndpoints.ServeAsync(
            "forum",
            "editors/x.webp",
            size: "thumb",
            stub,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, await GetStatusCodeAsync(result));
        Assert.Equal("editors/x-thumb.webp", stub.LastBlobName);
    }

    [Fact]
    public async Task ServeAsync_returns_not_found_for_path_traversal()
    {
        var result = await UgcProxyEndpoints.ServeAsync(
            "forum",
            "../secret.webp",
            size: null,
            new StubBlobUploadService(),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task ServeAsync_falls_back_to_full_when_thumb_missing()
    {
        var stub = new StubBlobUploadService
        {
            ContentByName = new Dictionary<string, BlobContent>(StringComparer.OrdinalIgnoreCase)
            {
                ["editors/x.webp"] = new BlobContent
                {
                    Stream = new MemoryStream([7]),
                    ContentType = "image/webp",
                },
            },
        };

        var result = await UgcProxyEndpoints.ServeAsync(
            "forum",
            "editors/x.webp",
            size: "thumb",
            stub,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, await GetStatusCodeAsync(result));
        Assert.Equal("editors/x.webp", stub.LastBlobName);
    }

    private async Task<int> GetStatusCodeAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext { RequestServices = factory.Services };
        await result.ExecuteAsync(httpContext);
        return httpContext.Response.StatusCode;
    }

    private sealed class StubBlobUploadService : IBlobUploadService
    {
        public BlobContent? Content { get; init; }

        public Dictionary<string, BlobContent>? ContentByName { get; init; }

        public string? LastContainer { get; private set; }

        public string? LastBlobName { get; private set; }

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
            CancellationToken cancellationToken = default)
        {
            LastContainer = containerName;
            LastBlobName = blobName;
            if (ContentByName is not null)
            {
                return Task.FromResult(
                    ContentByName.TryGetValue(blobName, out var named) ? named : null);
            }

            return Task.FromResult(Content);
        }
    }
}
