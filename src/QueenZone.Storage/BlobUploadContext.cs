namespace QueenZone.Storage;

/// <summary>
/// Optional ownership context for blob naming and policy selection.
/// Member uploads prefer <see cref="MemberId"/>; editorial uploads use <see cref="ActorEmail"/>.
/// </summary>
public sealed class BlobUploadContext
{
    public int? MemberId { get; init; }

    public string? ActorEmail { get; init; }
}
