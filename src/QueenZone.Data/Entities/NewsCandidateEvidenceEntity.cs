namespace QueenZone.Data.Entities;

public sealed class NewsCandidateEvidenceEntity
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string SourceName { get; set; } = string.Empty;

    public NewsDiscoveryTrustTier SourceTrustTier { get; set; }

    public string FetchedTitle { get; set; } = string.Empty;

    public DateTime? FetchedPublishedAt { get; set; }

    public string? Excerpt { get; set; }

    public string? ContentHash { get; set; }

    public DateTime FetchedAt { get; set; }

    public string? Etag { get; set; }

    public DateTime CreatedAt { get; set; }

    public NewsCandidateEntity Candidate { get; set; } = null!;
}
