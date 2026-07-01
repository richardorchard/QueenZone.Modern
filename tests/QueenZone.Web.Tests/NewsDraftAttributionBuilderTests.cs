using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDraftAttributionBuilderTests
{
  [Fact]
  public void Build_merges_model_and_evidence_sources()
  {
    var candidate = CreateCandidate();
    var source = CreateSource();
    IReadOnlyList<NewsCandidateEvidence> evidence =
    [
      new(
        1,
        7,
        "https://www.queenonline.com/news/tour-2026",
        "https://www.queenonline.com/news/tour-2026",
        "Queen Online",
        NewsDiscoveryTrustTier.Primary,
        "Queen announce 2026 tour",
        DateTime.UtcNow,
        "Official dates announced.",
        null,
        DateTime.UtcNow,
        null,
        DateTime.UtcNow)
    ];
    var structured = NewsDraftResultParser.Parse(NewsAgentTestSupport.SampleDraftJson);

    var attribution = NewsDraftAttributionBuilder.Build(structured, candidate, source, evidence);

    Assert.Contains("https://www.queenonline.com/news/tour-2026", attribution.SourceUrls);
    Assert.Contains("Queen Online", attribution.SourceNames);
    Assert.Contains("Queen Online", attribution.AttributionText, StringComparison.Ordinal);
  }

  [Fact]
  public void Build_adds_secondary_source_warning_to_confidence_notes()
  {
    var candidate = CreateCandidate();
    var source = CreateSource(NewsDiscoveryTrustTier.Secondary);
    var structured = NewsDraftResultParser.Parse("""
      {
        "title": "Queen documentary announced",
        "excerpt": "A new documentary is in production.",
        "body": "A new Queen documentary is reportedly in production.",
        "related_entities": ["Queen"],
        "source_urls": ["https://www.nme.com/music/queen-documentary"],
        "source_names": ["NME"],
        "attribution_text": "Source: NME",
        "confidence_notes": "Press report.",
        "source_notes": "Secondary music press.",
        "secondary_source_warning": true
      }
      """);

    var attribution = NewsDraftAttributionBuilder.Build(structured, candidate, source, []);

    Assert.Contains("secondary", attribution.ConfidenceNotes, StringComparison.OrdinalIgnoreCase);
  }

  private static NewsCandidate CreateCandidate() =>
    new(
      7,
      3,
      "https://www.queenonline.com/news/tour-2026",
      "https://www.queenonline.com/news/tour-2026",
      "hash",
      "Queen announce 2026 tour",
      DateTime.UtcNow,
      DateTime.UtcNow,
      "content-hash",
      NewsCandidateStatus.NeedsReview,
      0.91m,
      0.88m,
      null,
      null,
      null,
      DateTime.UtcNow,
      DateTime.UtcNow,
      "queen-online",
      "Queen Online",
      NewsDiscoveryTrustTier.Primary);

  private static NewsDiscoverySource CreateSource(NewsDiscoveryTrustTier trustTier = NewsDiscoveryTrustTier.Primary) =>
    new(
      3,
      trustTier == NewsDiscoveryTrustTier.Primary ? "queen-online" : "nme-music",
      trustTier == NewsDiscoveryTrustTier.Primary ? "Queen Online" : "NME Music",
      trustTier == NewsDiscoveryTrustTier.Primary ? "https://www.queenonline.com/" : "https://www.nme.com/music",
      trustTier == NewsDiscoveryTrustTier.Primary ? "https://www.queenonline.com/feed/" : "https://www.nme.com/feed",
      NewsDiscoverySourceType.Rss,
      trustTier,
      60,
      true,
      null,
      null,
      DateTime.UtcNow,
      DateTime.UtcNow);
}
