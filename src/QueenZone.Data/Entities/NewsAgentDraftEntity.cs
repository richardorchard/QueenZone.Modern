namespace QueenZone.Data.Entities;

public sealed class NewsAgentDraftEntity
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public int? AiRunId { get; set; }

    public string ProposedTitle { get; set; } = string.Empty;

    public string? ProposedSlug { get; set; }

    public string ProposedExcerpt { get; set; } = string.Empty;

    public string ProposedBody { get; set; } = string.Empty;

    public string? AttributionText { get; set; }

    public string? SourceNotes { get; set; }

    public string? ConfidenceNotes { get; set; }

    public DateTime? SuggestedPublishAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public NewsCandidateEntity Candidate { get; set; } = null!;

    public NewsAiRunEntity? AiRun { get; set; }
}
