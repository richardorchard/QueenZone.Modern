namespace QueenZone.Data.Entities;

public sealed class NewsAiRunEntity
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public NewsAiRunKind Kind { get; set; }

    public string ModelProvider { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

    public NewsAiRunStatus Status { get; set; }

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public decimal? EstimatedCostUsd { get; set; }

    public string? StructuredResultJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public NewsCandidateEntity Candidate { get; set; } = null!;
}
