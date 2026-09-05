using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using QueenZone.Data;

namespace QueenZone.Tools;

internal sealed record PhotoCandidate(int Id, int CategoryId, string? Url, string? ThumbUrl, bool Required);

internal sealed record PhotoSelection(IReadOnlyList<int> PhotoIds, IReadOnlyList<SnapshotBlob> Blobs);

[ExcludeFromCodeCoverage]
internal sealed class BlobSnapshotService(
    DevSnapshotConfig config,
    string sourceConnectionString,
    string targetConnectionString)
{
    private readonly BlobServiceClient source = new(sourceConnectionString);
    private readonly BlobServiceClient target = new(targetConnectionString);

    public async Task<PhotoSelection> SelectPhotosAsync(IReadOnlyList<PhotoCandidate> candidates)
    {
        var ids = new List<int>();
        var blobs = new List<SnapshotBlob>();
        var acceptedBytes = 0L;

        foreach (var candidate in candidates)
        {
            var candidateBlobs = new List<SnapshotBlob>();
            foreach (var (path, kind) in new[] { (candidate.Url, "gallery-original"), (candidate.ThumbUrl, "gallery-thumbnail") })
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    candidateBlobs.Clear();
                    break;
                }

                var location = ParseGalleryLocation(path);
                var blob = await ResolveAsync(location.Container, location.Name, "gallery", $"PIC_FILES_T:{candidate.Id}:{kind}");
                if (blob is null)
                {
                    candidateBlobs.Clear();
                    break;
                }

                candidateBlobs.Add(blob);
            }

            if (candidateBlobs.Count == 0)
            {
                if (candidate.Required)
                {
                    throw new InvalidOperationException($"Required gallery photo {candidate.Id} has missing assets.");
                }
                continue;
            }

            var newBytes = candidateBlobs.Sum(blob => blob.Bytes);
            if (acceptedBytes + newBytes > config.GalleryBudgetBytes)
            {
                if (candidate.Required)
                {
                    throw new InvalidOperationException($"Required gallery photo {candidate.Id} exceeds the gallery budget.");
                }
                continue;
            }

            acceptedBytes += newBytes;
            ids.Add(candidate.Id);
            blobs.AddRange(candidateBlobs);
        }

        var missingCategories = candidates.Select(item => item.CategoryId).Distinct()
            .Except(candidates.Where(item => ids.Contains(item.Id)).Select(item => item.CategoryId))
            .ToArray();
        if (missingCategories.Length > 0)
        {
            throw new InvalidOperationException($"No valid in-budget photo was found for categories: {string.Join(", ", missingCategories)}.");
        }

        return new PhotoSelection(ids, blobs);
    }

    public async Task<IReadOnlyList<SnapshotBlob>> GetForumAndEditorialBlobsAsync(SqlSnapshotCopySession session)
    {
        var references = await session.GetBlobReferencesAsync();
        var result = new List<SnapshotBlob>();
        foreach (var reference in references)
        {
            var blob = await ResolveAsync(reference.Container, reference.Name, reference.Budget, reference.Source);
            if (blob is null)
            {
                throw new InvalidOperationException($"Referenced source blob is missing: {reference.Container}/{reference.Name} ({reference.Source}).");
            }

            result.Add(blob);
        }

        return result;
    }

    public void EnsureBudgets(IEnumerable<SnapshotBlob> manifest)
    {
        var gallery = manifest.Where(blob => blob.Budget == "gallery").Sum(blob => blob.Bytes);
        var forum = manifest.Where(blob => blob.Budget == "forum").Sum(blob => blob.Bytes);
        if (gallery > config.GalleryBudgetBytes)
        {
            throw new InvalidOperationException($"Gallery blob budget exceeded: {gallery} > {config.GalleryBudgetBytes} bytes.");
        }

        if (forum > config.ForumAttachmentBudgetBytes)
        {
            throw new InvalidOperationException($"Forum attachment budget exceeded: {forum} > {config.ForumAttachmentBudgetBytes} bytes.");
        }
    }

    public async Task ResetTargetAndCopyAsync(IReadOnlyList<SnapshotBlob> manifest)
    {
        await foreach (var container in target.GetBlobContainersAsync())
        {
            var client = target.GetBlobContainerClient(container.Name);
            await foreach (var blob in client.GetBlobsAsync())
            {
                await client.DeleteBlobIfExistsAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots);
            }
        }

        foreach (var item in manifest)
        {
            var sourceBlob = source.GetBlobContainerClient(item.Container).GetBlobClient(item.Name);
            var targetContainer = target.GetBlobContainerClient(item.Container);
            await targetContainer.CreateIfNotExistsAsync();
            var targetBlob = targetContainer.GetBlobClient(item.Name);
            var properties = await sourceBlob.GetPropertiesAsync();
            await using var content = await sourceBlob.OpenReadAsync();
            await targetBlob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = properties.Value.ContentType,
                    CacheControl = properties.Value.CacheControl,
                    ContentDisposition = properties.Value.ContentDisposition,
                },
            });
        }
    }

    private async Task<SnapshotBlob?> ResolveAsync(string container, string name, string budget, string reference)
    {
        try
        {
            var properties = await source.GetBlobContainerClient(container).GetBlobClient(name).GetPropertiesAsync();
            return new SnapshotBlob(container, name, budget, properties.Value.ContentLength, reference);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private static (string Container, string Name) ParseGalleryLocation(string path)
    {
        var absolute = Uri.TryCreate(path, UriKind.Absolute, out _)
            ? path
            : PhotoImageUrl.BuildBlobStorageUrl(path);
        if (!PhotoImageUrl.TryParseBlobLocation(absolute, out var container, out var name))
        {
            throw new InvalidOperationException($"Invalid gallery blob path: {path}");
        }

        return (container, name);
    }
}
