namespace QueenZone.Data.Entities;

public sealed class NewsCandidateEntity
{
    public int Id { get; set; }

    public int SourceId { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string CanonicalUrlHash { get; set; } = string.Empty;

    public string SourceTitle { get; set; } = string.Empty;

    public DateTime? SourcePublishedAt { get; set; }

    public DateTime DiscoveredAt { get; set; }

    public string? ContentHash { get; set; }

    public NewsCandidateStatus Status { get; set; }

    public decimal? RelevanceScore { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public int? DuplicateOfCandidateId { get; set; }

    public int? PromotedNewsId { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public NewsDiscoverySourceEntity Source { get; set; } = null!;

    public NewsCandidateEntity? DuplicateOfCandidate { get; set; }

    public ICollection<NewsCandidateEvidenceEntity> Evidence { get; set; } = [];

    public ICollection<NewsAiRunEntity> AiRuns { get; set; } = [];

    public NewsAgentDraftEntity? Draft { get; set; }
}
