using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Storage;

/// <summary>
/// Uploads and reads public gallery blobs in legacy CDN containers (not UGC containers).
/// </summary>
public interface IGalleryPhotoBlobService
{
    Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    bool IsConfigured { get; }
}

public sealed class NullGalleryPhotoBlobService : IGalleryPhotoBlobService
{
    private readonly Dictionary<string, byte[]> blobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly object sync = new();

    public bool IsConfigured => true;

    public Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        lock (sync)
        {
            blobs[Key(containerName, blobName)] = buffer.ToArray();
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            if (!blobs.TryGetValue(Key(containerName, blobName), out var bytes))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
        }
    }

    private static string Key(string containerName, string blobName) =>
        $"{containerName}/{blobName}";
}

public sealed class AzureGalleryPhotoBlobService(BlobServiceClient blobServiceClient) : IGalleryPhotoBlobService
{
    public bool IsConfigured => true;

    public async Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var blob = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blob = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }
}

public static class GalleryPhotoBlobServiceCollectionExtensions
{
    public static IServiceCollection AddGalleryPhotoBlobService(this IServiceCollection services)
    {
        services.AddSingleton<IGalleryPhotoBlobService>(sp =>
        {
            var blobServiceClient = sp.GetService<BlobServiceClient>();
            return blobServiceClient is null
                ? new NullGalleryPhotoBlobService()
                : new AzureGalleryPhotoBlobService(blobServiceClient);
        });
        return services;
    }
}
