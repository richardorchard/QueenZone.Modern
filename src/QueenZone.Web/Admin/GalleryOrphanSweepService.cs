using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

public sealed record GalleryOrphanSweepResult(
    int BlobsScanned,
    int OrphansFound,
    int OrphansDeleted,
    int DeleteFailures);

/// <summary>
/// Compares gallery blob storage against <c>PIC_FILES_T</c> per category and removes (or, in
/// dry-run mode, reports) blobs with no referencing row that are older than the configured
/// grace period. Complements the compensating delete in
/// <see cref="PhotoSubmissionPromotionService"/> (#590) by catching orphans left behind by the
/// residual crash window between a successful upload and that compensating delete (#651).
/// </summary>
public sealed class GalleryOrphanSweepService(
    IAdminPhotoRepository adminPhotoRepository,
    IGalleryPhotoBlobService galleryPhotoBlobService,
    TimeProvider timeProvider,
    IOptions<GalleryOrphanSweepOptions> options,
    ILogger<GalleryOrphanSweepService> logger)
{
    public async Task<GalleryOrphanSweepResult> SweepAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow() - TimeSpan.FromMinutes(options.Value.GracePeriodMinutes);
        var categories = await adminPhotoRepository.GetCategoriesAsync(cancellationToken);

        var scanned = 0;
        var found = 0;
        var deleted = 0;
        var failures = 0;

        foreach (var category in categories)
        {
            var container = PhotoLegacyPath.BlobContainerName(category.Name);
            var blobs = await galleryPhotoBlobService.ListBlobsAsync(container, cancellationToken);
            if (blobs.Count == 0)
            {
                continue;
            }

            scanned += blobs.Count;
            var referenced = await adminPhotoRepository.GetReferencedBlobNamesAsync(category.CatId, cancellationToken);
            var referencedNames = new HashSet<string>(referenced, StringComparer.OrdinalIgnoreCase);

            foreach (var blob in blobs)
            {
                if (referencedNames.Contains(blob.BlobName) || blob.LastModified > cutoff)
                {
                    continue;
                }

                found++;
                if (options.Value.DryRun)
                {
                    logger.LogInformation(
                        "Orphan gallery blob detected (dry run): {Container}/{BlobName}, last modified {LastModified}",
                        container,
                        blob.BlobName,
                        blob.LastModified);
                    continue;
                }

                try
                {
                    await galleryPhotoBlobService.DeleteAsync(container, blob.BlobName, cancellationToken);
                    deleted++;
                    logger.LogInformation(
                        "Deleted orphan gallery blob {Container}/{BlobName}", container, blob.BlobName);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failures++;
                    logger.LogWarning(
                        ex, "Failed to delete orphan gallery blob {Container}/{BlobName}", container, blob.BlobName);
                }
            }
        }

        return new GalleryOrphanSweepResult(scanned, found, deleted, failures);
    }
}
