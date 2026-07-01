using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsDraftGenerationOptions
{
    public const string SectionName = "NewsDraftGeneration";

    public decimal PrimaryMinConfidenceScore { get; set; } = 0.55m;

    public decimal SecondaryMinConfidenceScore { get; set; } = 0.75m;

    public int PerRunCandidateLimit { get; set; } = 10;

    public void Validate()
    {
        if (PerRunCandidateLimit <= 0)
        {
            throw new InvalidOperationException("NewsDraftGeneration PerRunCandidateLimit must be greater than zero.");
        }

        ValidateScore(nameof(PrimaryMinConfidenceScore), PrimaryMinConfidenceScore);
        ValidateScore(nameof(SecondaryMinConfidenceScore), SecondaryMinConfidenceScore);
    }

    public decimal MinConfidenceScore(NewsDiscoveryTrustTier trustTier) =>
        trustTier == NewsDiscoveryTrustTier.Primary
            ? PrimaryMinConfidenceScore
            : SecondaryMinConfidenceScore;

    private static void ValidateScore(string name, decimal value)
    {
        if (value is < 0m or > 1m)
        {
            throw new InvalidOperationException($"{name} must be between 0 and 1.");
        }
    }
}
