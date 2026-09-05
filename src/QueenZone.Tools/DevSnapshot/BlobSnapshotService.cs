using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using QueenZone.Data;

namespace QueenZone.Tools;

internal sealed record PhotoCandidate(int Id, int CategoryId, string? Url, string? ThumbUrl, bool Required);

internal sealed record PhotoSelection(IReadOnlyList<int> PhotoIds, IReadOnlyList<SnapshotBlob> Blobs);

internal sealed record MissingForumBlobReference(long? LegacyPostId, Guid? AttachmentId);

internal sealed record MissingEditorialBlobReference(int? LegacyNewsId, Guid? EditorialArticleId, bool IsLive);

[ExcludeFromCodeCoverage]
internal sealed record ReferencedBlobSelection(
    IReadOnlyList<SnapshotBlob> Blobs,
    IReadOnlyList<MissingForumBlobReference> MissingForumBlobs,
    IReadOnlyList<MissingEditorialBlobReference> MissingEditorialBlobs);

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

                if (!TryParseGalleryLocation(path, out var container, out var name))
                {
                    candidateBlobs.Clear();
                    break;
                }

                var blob = await ResolveAsync(container, name, "gallery", $"PIC_FILES_T:{candidate.Id}:{kind}");
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

    public async Task<ReferencedBlobSelection> GetForumAndEditorialBlobsAsync(SqlSnapshotCopySession session)
    {
        var references = await session.GetBlobReferencesAsync();
        var result = new List<SnapshotBlob>();
        var missingForumBlobs = new List<MissingForumBlobReference>();
        var missingEditorialBlobs = new List<MissingEditorialBlobReference>();
        foreach (var reference in references)
        {
            var blob = await ResolveAsync(reference.Container, reference.Name, reference.Budget, reference.Source);
            if (blob is null)
            {
                if (string.Equals(reference.Budget, "forum", StringComparison.OrdinalIgnoreCase))
                {
                    missingForumBlobs.Add(ParseMissingForumBlobReference(reference.Source));
                    continue;
                }

                missingEditorialBlobs.Add(ParseMissingEditorialBlobReference(reference.Source));
                continue;
            }

            result.Add(blob);
        }

        return new ReferencedBlobSelection(result, missingForumBlobs, missingEditorialBlobs);
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

    internal static bool TryParseGalleryLocation(string path, out string container, out string name)
    {
        var isHttpUrl = Uri.TryCreate(path, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (uri?.IsAbsoluteUri == true && !isHttpUrl && !path.StartsWith("/", StringComparison.Ordinal))
        {
            container = string.Empty;
            name = string.Empty;
            return false;
        }

        var absolute = isHttpUrl ? path : PhotoImageUrl.BuildBlobStorageUrl(path);
        return PhotoImageUrl.TryParseBlobLocation(absolute, out container, out name);
    }

    internal static MissingForumBlobReference ParseMissingForumBlobReference(string source)
    {
        const string legacyPrefix = "ModernForumPost:";
        if (source.StartsWith(legacyPrefix, StringComparison.Ordinal)
            && long.TryParse(source[legacyPrefix.Length..], out var legacyPostId))
        {
            return new MissingForumBlobReference(legacyPostId, null);
        }

        const string attachmentPrefix = "ForumPostAttachments:";
        if (source.StartsWith(attachmentPrefix, StringComparison.Ordinal)
            && Guid.TryParse(source[attachmentPrefix.Length..], out var attachmentId))
        {
            return new MissingForumBlobReference(null, attachmentId);
        }

        throw new InvalidOperationException($"Unknown forum blob reference source: {source}.");
    }

    internal static MissingEditorialBlobReference ParseMissingEditorialBlobReference(string source)
    {
        const string newsPrefix = "NEWS_T:";
        if (source.StartsWith(newsPrefix, StringComparison.Ordinal)
            && int.TryParse(source[newsPrefix.Length..], out var newsId))
        {
            return new MissingEditorialBlobReference(newsId, null, false);
        }

        const string articlePrefix = "EditorialArticles:";
        if (source.StartsWith(articlePrefix, StringComparison.Ordinal)
            && Guid.TryParse(source[articlePrefix.Length..], out var articleId))
        {
            return new MissingEditorialBlobReference(null, articleId, false);
        }

        const string liveArticlePrefix = "EditorialArticles-live:";
        if (source.StartsWith(liveArticlePrefix, StringComparison.Ordinal)
            && Guid.TryParse(source[liveArticlePrefix.Length..], out var liveArticleId))
        {
            return new MissingEditorialBlobReference(null, liveArticleId, true);
        }

        throw new InvalidOperationException($"Unknown editorial blob reference source: {source}.");
    }
}
