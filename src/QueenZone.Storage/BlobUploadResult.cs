namespace QueenZone.Storage;

/// <summary>
/// Durable storage key plus optional display URL. Prefer storing container + blob name in the database;
/// <see cref="PublicUrl"/> may be a CDN form or short-lived SAS and can change over time.
/// </summary>
public sealed class BlobUploadResult
{
    public required string Container { get; init; }

    public required string BlobName { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>
    /// Optional display URL (SAS, CDN, or storage URI). Not a stable product contract for deletes.
    /// </summary>
    public string? PublicUrl { get; init; }
}
