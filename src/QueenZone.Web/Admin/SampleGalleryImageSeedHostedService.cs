using QueenZone.Data;
using QueenZone.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web;

/// <summary>
/// Writes deterministic JPEG stand-ins for sample PIC originals into the in-memory
/// gallery blob store so local/Testing admin crop can read the same bytes save uses.
/// Registered only with in-memory data. Never runs against Azure Blob.
/// </summary>
public sealed class SampleGalleryImageSeedHostedService(
    IGalleryPhotoBlobService galleryPhotoBlobService,
    ILogger<SampleGalleryImageSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var category in SamplePhotoData.CreateSeedCategories())
            {
                foreach (var item in category.Items)
                {
                    var blobUrl = PhotoImageUrl.ToBlobStorageUrl(item.Url);
                    if (!PhotoImageUrl.TryParseBlobLocation(blobUrl, out var container, out var blobName))
                    {
                        continue;
                    }

                    await using var existing = await galleryPhotoBlobService.OpenReadAsync(
                        container,
                        blobName,
                        cancellationToken);
                    if (existing is not null)
                    {
                        continue;
                    }

                    var width = item.PictureWidth >= NewsArticleImageProcessor.MinCropWidth
                        ? item.PictureWidth
                        : 600;
                    var height = item.PictureHeight >= NewsArticleImageProcessor.MinCropHeight
                        ? item.PictureHeight
                        : 400;
                    await using var jpeg = await CreateJpegAsync(width, height, cancellationToken);
                    await galleryPhotoBlobService.UploadAsync(
                        container,
                        blobName,
                        jpeg,
                        "image/jpeg",
                        cancellationToken);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Sample gallery image seeding failed; gallery crop will 404 until blobs exist.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<MemoryStream> CreateJpegAsync(int width, int height, CancellationToken cancellationToken)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new JpegEncoder(), cancellationToken);
        stream.Position = 0;
        return stream;
    }
}
