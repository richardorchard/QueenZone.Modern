using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class NewsSuggestionEntity
{
    public Guid Id { get; set; }

    public Guid SubmitterMemberId { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of <see cref="NewsCandidateDedupe.NormalizeCanonicalUrl"/> for deduplication.</summary>
    public string UrlHash { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = NewsSuggestionStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewerEmail { get; set; }

    public string? ReviewNotes { get; set; }

    public int? PromotedNewsId { get; set; }

    public int? DuplicateCandidateId { get; set; }

    public MemberAccount? Submitter { get; set; }

    public NewsCandidateEntity? DuplicateCandidate { get; set; }
}
