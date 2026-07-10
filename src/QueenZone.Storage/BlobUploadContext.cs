namespace QueenZone.Storage;

/// <summary>
/// Optional ownership context for blob naming and policy selection.
/// Modern member uploads prefer <see cref="MemberAccountId"/>; legacy int <see cref="MemberId"/>
/// remains for older callers; editorial uploads use <see cref="ActorEmail"/>.
/// </summary>
public sealed class BlobUploadContext
{
    /// <summary>
    /// Modern member account id (Guid). Preferred for avatar / member UGC paths.
    /// </summary>
    public Guid? MemberAccountId { get; init; }

    /// <summary>
    /// Legacy numeric member id used by older call sites.
    /// </summary>
    public int? MemberId { get; init; }

    public string? ActorEmail { get; init; }

    /// <summary>
    /// When set, used as the exact blob name instead of generating one.
    /// </summary>
    public string? PreferredBlobName { get; init; }
}
