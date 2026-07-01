using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDraftPromptTests
{
  [Fact]
  public void BuildMessages_includes_candidate_and_evidence_context()
  {
    var candidate = new NewsCandidate(
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
    var source = new NewsDiscoverySource(
      3,
      "queen-online",
      "Queen Online",
      "https://www.queenonline.com/",
      "https://www.queenonline.com/feed/",
      NewsDiscoverySourceType.Rss,
      NewsDiscoveryTrustTier.Primary,
      60,
      true,
      null,
      null,
      DateTime.UtcNow,
      DateTime.UtcNow);
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

    var messages = NewsDraftPrompt.BuildMessages(candidate, source, evidence);

    Assert.Equal(2, messages.Count);
    Assert.Contains("Queen announce 2026 tour", messages[1].Content, StringComparison.Ordinal);
    Assert.Equal("draft-v1", NewsDraftPrompt.Version);
  }
}
