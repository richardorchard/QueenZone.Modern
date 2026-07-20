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
    /// Max size for rich-text editor uploads (paste / toolbar / drag-drop). Defaults to 10 MB.
    /// The effective limit is the minimum of this value and the target container's max.
    /// </summary>
    public long EditorMaxBytes { get; set; } = 10 * 1024 * 1024;

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
                MaxBytes = 20 * 1024 * 1024,
                AllowedContentTypes =
                [
                    "image/jpeg",
                    "image/png",
                    "image/gif",
                    "image/webp",
                    "application/pdf",
                    "text/plain",
                    "application/zip",
                    "application/x-zip-compressed",
                    "audio/mpeg",
                    "audio/mp3",
                    "audio/flac",
                    "audio/x-flac",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/vnd.ms-excel",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/vnd.ms-powerpoint",
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ],
            },
            [BlobUploadContainers.Photos] = new()
            {
                MaxBytes = 20 * 1024 * 1024,
                AllowedContentTypes =
                [
                    "image/jpeg",
                    "image/png",
                    "image/webp",
                    "image/tiff",
                ],
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
