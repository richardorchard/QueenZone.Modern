using SixLabors.ImageSharp;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Orchestrates gallery admin uploads, replacements, and WebP thumbnail regeneration.
/// </summary>
public sealed class AdminPhotoService(
    IAdminPhotoRepository adminPhotoRepository,
    IGalleryPhotoBlobService galleryPhotoBlobService)
{
    public async Task<int> CreateAsync(
        IFormFile file,
        int catId,
        string title,
        string? keywords,
        int year,
        DateTime dateTime,
        bool isVisible,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        var category = await adminPhotoRepository.GetCategoryByIdAsync(catId, cancellationToken)
            ?? throw new InvalidOperationException("Category was not found.");

        await using var uploadStream = file.OpenReadStream();
        var processed = await PhotoSubmissionImageProcessor.ProcessAsync(
            uploadStream,
            file.FileName,
            cancellationToken);

        try
        {
            var stem = Guid.NewGuid().ToString("N");
            var originalExtension = ResolveOriginalExtension(processed.MimeType, file.FileName);
            var originalFileName = stem + originalExtension;
            var thumbFileName = PhotoWebpDerivatives.ToThumbnailBlobName(originalFileName);
            var container = PhotoLegacyPath.BlobContainerName(category.Name);
            var legacyUrl = PhotoLegacyPath.BuildLegacyPath(category.Name, originalFileName);
            var legacyThumbUrl = PhotoLegacyPath.BuildLegacyPath(category.Name, thumbFileName);
            var thumbSize = PhotoWebpDerivatives.DefaultThumbSizePixels;

            processed.Original.Position = 0;
            await galleryPhotoBlobService.UploadAsync(
                container,
                originalFileName,
                processed.Original,
                processed.MimeType,
                cancellationToken);

            processed.Thumbnail.Position = 0;
            await galleryPhotoBlobService.UploadAsync(
                container,
                thumbFileName,
                processed.Thumbnail,
                PhotoWebpDerivatives.WebpContentType,
                cancellationToken);

            return await adminPhotoRepository.CreateAsync(
                new AdminPhotoCreateRequest(
                    catId,
                    title,
                    keywords,
                    year,
                    dateTime,
                    isVisible,
                    legacyUrl,
                    legacyThumbUrl,
                    thumbSize,
                    thumbSize,
                    processed.WidthPx,
                    processed.HeightPx),
                editorEmail,
                cancellationToken);
        }
        finally
        {
            await processed.Original.DisposeAsync();
            await processed.WebOptimized.DisposeAsync();
            await processed.Thumbnail.DisposeAsync();
        }
    }

    public async Task ReplaceAsync(
        int picId,
        IFormFile file,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        var existing = await adminPhotoRepository.GetByIdAsync(picId, cancellationToken)
            ?? throw new InvalidOperationException($"Photo {picId} was not found.");

        await using var uploadStream = file.OpenReadStream();
        var processed = await PhotoSubmissionImageProcessor.ProcessAsync(
            uploadStream,
            file.FileName,
            cancellationToken);

        try
        {
            var stem = Guid.NewGuid().ToString("N");
            var originalExtension = ResolveOriginalExtension(processed.MimeType, file.FileName);
            var originalFileName = stem + originalExtension;
            var thumbFileName = PhotoWebpDerivatives.ToThumbnailBlobName(originalFileName);
            var container = PhotoLegacyPath.BlobContainerName(existing.CategoryName);
            var legacyUrl = PhotoLegacyPath.BuildLegacyPath(existing.CategoryName, originalFileName);
            var legacyThumbUrl = PhotoLegacyPath.BuildLegacyPath(existing.CategoryName, thumbFileName);
            var thumbSize = PhotoWebpDerivatives.DefaultThumbSizePixels;

            processed.Original.Position = 0;
            await galleryPhotoBlobService.UploadAsync(
                container,
                originalFileName,
                processed.Original,
                processed.MimeType,
                cancellationToken);

            processed.Thumbnail.Position = 0;
            await galleryPhotoBlobService.UploadAsync(
                container,
                thumbFileName,
                processed.Thumbnail,
                PhotoWebpDerivatives.WebpContentType,
                cancellationToken);

            await adminPhotoRepository.UpdateAssetsAsync(
                picId,
                new AdminPhotoAssetUpdate(
                    legacyUrl,
                    legacyThumbUrl,
                    thumbSize,
                    thumbSize,
                    processed.WidthPx,
                    processed.HeightPx),
                editorEmail,
                cancellationToken);
        }
        finally
        {
            await processed.Original.DisposeAsync();
            await processed.WebOptimized.DisposeAsync();
            await processed.Thumbnail.DisposeAsync();
        }
    }

    public async Task RegenerateThumbnailAsync(
        int picId,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        var existing = await adminPhotoRepository.GetByIdAsync(picId, cancellationToken)
            ?? throw new InvalidOperationException($"Photo {picId} was not found.");

        var blobUrl = PhotoImageUrl.ToBlobStorageUrl(existing.LegacyUrl);
        if (!PhotoImageUrl.TryParseBlobLocation(blobUrl, out var container, out var blobName))
        {
            throw new InvalidOperationException($"Could not parse blob location from Url '{existing.LegacyUrl}'.");
        }

        await using var source = await galleryPhotoBlobService.OpenReadAsync(container, blobName, cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Source blob not found: {container}/{blobName}");
        }

        using var image = await Image.LoadAsync(source, cancellationToken);
        await using var thumb = await PhotoWebpDerivatives.CreateSquareThumbnailAsync(
            image,
            cancellationToken: cancellationToken);

        var thumbBlobName = PhotoWebpDerivatives.ToThumbnailBlobName(blobName);
        var legacyThumbPath = PhotoWebpDerivatives.ToLegacyThumbnailPath(existing.LegacyUrl, thumbBlobName);

        thumb.Stream.Position = 0;
        await galleryPhotoBlobService.UploadAsync(
            container,
            thumbBlobName,
            thumb.Stream,
            PhotoWebpDerivatives.WebpContentType,
            cancellationToken);

        await adminPhotoRepository.UpdateThumbnailAsync(
            picId,
            legacyThumbPath,
            thumb.WidthPx,
            thumb.HeightPx,
            editorEmail,
            cancellationToken);
    }

    private static string ResolveOriginalExtension(string mimeType, string fileName)
    {
        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/tiff" => ".tif",
            _ => Path.GetExtension(fileName) is { Length: > 0 } ext ? ext.ToLowerInvariant() : ".jpg",
        };
    }
}
