using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsTriageOptions
{
    public const string SectionName = "NewsTriage";

    public decimal PrimaryMinRelevanceScore { get; set; } = 0.55m;

    public decimal SecondaryMinRelevanceScore { get; set; } = 0.75m;

    public decimal PrimaryMinConfidenceScore { get; set; } = 0.55m;

    public decimal SecondaryMinConfidenceScore { get; set; } = 0.75m;

    public decimal MaybeRelevantConfidencePenalty { get; set; } = 0.10m;

    public int PerRunCandidateLimit { get; set; } = 25;

    public void Validate()
    {
        if (PerRunCandidateLimit <= 0)
        {
            throw new InvalidOperationException("NewsTriage PerRunCandidateLimit must be greater than zero.");
        }

        ValidateScore(nameof(PrimaryMinRelevanceScore), PrimaryMinRelevanceScore);
        ValidateScore(nameof(SecondaryMinRelevanceScore), SecondaryMinRelevanceScore);
        ValidateScore(nameof(PrimaryMinConfidenceScore), PrimaryMinConfidenceScore);
        ValidateScore(nameof(SecondaryMinConfidenceScore), SecondaryMinConfidenceScore);
        ValidateScore(nameof(MaybeRelevantConfidencePenalty), MaybeRelevantConfidencePenalty);
    }

    private static void ValidateScore(string name, decimal value)
    {
        if (value is < 0m or > 1m)
        {
            throw new InvalidOperationException($"{name} must be between 0 and 1.");
        }
    }

    public decimal MinRelevanceScore(NewsDiscoveryTrustTier trustTier) =>
        trustTier == NewsDiscoveryTrustTier.Primary
            ? PrimaryMinRelevanceScore
            : SecondaryMinRelevanceScore;

    public decimal MinConfidenceScore(NewsDiscoveryTrustTier trustTier) =>
        trustTier == NewsDiscoveryTrustTier.Primary
            ? PrimaryMinConfidenceScore
            : SecondaryMinConfidenceScore;
}
