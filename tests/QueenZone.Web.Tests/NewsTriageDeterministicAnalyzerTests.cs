using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsTriageDeterministicAnalyzerTests
{
    [Fact]
    public async Task Analyze_detects_title_duplicate_candidate()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var sourceId = await repository.UpsertSourceAsync(CreatePrimarySource());
        var firstId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Official dates announced.",
            DateTime.UtcNow));
        var secondId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026-copy",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Different excerpt.",
            DateTime.UtcNow.AddMinutes(1)));

        var source = (await repository.GetSourceByIdAsync(sourceId))!;
        var first = (await repository.GetCandidateByIdAsync(secondId))!;
        var evidence = await repository.GetCandidateEvidenceAsync(secondId);
        var analyzer = new NewsTriageDeterministicAnalyzer(repository);

        var signals = await analyzer.AnalyzeAsync(first, source, evidence);

        Assert.Equal(firstId, signals.DuplicateOfCandidateId);
    }

    [Fact]
    public async Task Analyze_requires_keyword_match_for_secondary_source()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "nme-music",
            "NME Music",
            "https://www.nme.com/music",
            "https://www.nme.com/feed",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Secondary,
            60,
            true,
            "queen"));
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.nme.com/music/pedal-review",
            "Generic guitar pedal review",
            DateTime.UtcNow,
            "No band mentioned.",
            DateTime.UtcNow));

        var source = (await repository.GetSourceByIdAsync(sourceId))!;
        var candidate = (await repository.GetCandidateByIdAsync(candidateId))!;
        var analyzer = new NewsTriageDeterministicAnalyzer(repository);

        var signals = await analyzer.AnalyzeAsync(candidate, source, []);

        Assert.False(signals.KeywordMatch);
    }

    private static NewsDiscoverySourceDraft CreatePrimarySource() =>
        new(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed/",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null);
}
