namespace QueenZone.Storage;

public sealed class BlobUploadOptions
{
    public const string SectionName = "BlobUpload";

    public const string ConnectionStringName = "BlobStorage";

    /// <summary>
    /// Default max payload size when a container has no override (10 MB).
    /// </summary>
    public long DefaultMaxBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Default allowed MIME types (images). Containers may override.
    /// </summary>
    public List<string> DefaultAllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
    ];

    /// <summary>
    /// Per-container policy overrides keyed by container name (e.g. ugc-avatars).
    /// </summary>
    public Dictionary<string, BlobContainerPolicy> Containers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [BlobUploadContainers.Avatars] = new()
            {
                MaxBytes = 2 * 1024 * 1024,
                AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"],
            },
            [BlobUploadContainers.Forum] = new()
            {
                MaxBytes = 10 * 1024 * 1024,
                AllowedContentTypes =
                [
                    "image/jpeg",
                    "image/png",
                    "image/gif",
                    "image/webp",
                    "application/pdf",
                    "text/plain",
                ],
            },
            [BlobUploadContainers.Photos] = new()
            {
                MaxBytes = 15 * 1024 * 1024,
                AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"],
            },
            [BlobUploadContainers.Articles] = new()
            {
                MaxBytes = 10 * 1024 * 1024,
                AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"],
            },
        };

    /// <summary>
    /// Optional base URL used when building a display URL (CDN / worker). When empty, the storage
    /// URI is returned as <see cref="BlobUploadResult.PublicUrl"/> (not the preferred public contract).
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}

public sealed class BlobContainerPolicy
{
    public long? MaxBytes { get; set; }

    public List<string>? AllowedContentTypes { get; set; }
}
