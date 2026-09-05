using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Tools;

internal sealed class SnapshotSizeException(string message) : InvalidOperationException(message);

[ExcludeFromCodeCoverage]
internal sealed record SnapshotBlob(
    string Container,
    string Name,
    string Budget,
    long Bytes,
    string Source);

[ExcludeFromCodeCoverage]
internal sealed record SnapshotSummary(
    DateTimeOffset CreatedAt,
    string SourceDatabase,
    string TargetDatabase,
    int ForumCategories,
    int ForumThreads,
    int ForumPosts,
    int Photos,
    int Members,
    int LegacyUsers,
    int BlobCount,
    long GalleryBlobBytes,
    long ForumAttachmentBytes,
    decimal DatabaseUsedMb,
    IReadOnlyDictionary<string, long> TableRows);
