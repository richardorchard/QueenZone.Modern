using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class NewsTriagePromptTests
{
    [Fact]
    public void BuildMessages_includes_candidate_source_and_evidence_context()
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
            NewsCandidateStatus.Discovered,
            null,
            null,
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

        var messages = NewsTriagePrompt.BuildMessages(candidate, source, evidence);

        Assert.Equal(2, messages.Count);
        Assert.Contains("Queen announce 2026 tour", messages[1].Content, StringComparison.Ordinal);
        Assert.Contains("triage", messages[0].Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("triage-v2", NewsTriagePrompt.Version);
        Assert.Contains("Tony Iommi", messages[0].Content, StringComparison.Ordinal);
        Assert.Contains("guest performance", messages[0].Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("substantive, attributable comments", messages[0].Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("should be preserved for review", messages[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildMessages_without_guidance_is_byte_identical_to_compiled_prompt()
    {
        var (candidate, source, evidence) = CreatePromptContext();
        var withoutArgument = NewsTriagePrompt.BuildMessages(candidate, source, evidence);
        var withNull = NewsTriagePrompt.BuildMessages(candidate, source, evidence, null);
        var withEmpty = NewsTriagePrompt.BuildMessages(candidate, source, evidence, "   ");

        Assert.Equal(withoutArgument[0].Content, withNull[0].Content);
        Assert.Equal(withoutArgument[0].Content, withEmpty[0].Content);
        Assert.Equal(NewsTriagePrompt.BuildCompiledSystemPrompt(), withoutArgument[0].Content);
        Assert.DoesNotContain(NewsAgentEditorialGuidance.BeginMarker, withoutArgument[0].Content, StringComparison.Ordinal);
        Assert.Equal("triage-v2", NewsTriagePrompt.Version);
    }

    [Fact]
    public void BuildMessages_uses_published_prompt_as_complete_system_prompt()
    {
        const string publishedPrompt = "Custom triage system prompt";
        var (candidate, source, evidence) = CreatePromptContext();
        var messages = NewsTriagePrompt.BuildMessages(candidate, source, evidence, publishedPrompt);

        Assert.Equal(publishedPrompt, messages[0].Content);
    }

    private static (NewsCandidate Candidate, NewsDiscoverySource Source, IReadOnlyList<NewsCandidateEvidence> Evidence) CreatePromptContext()
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
            NewsCandidateStatus.Discovered,
            null,
            null,
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
        return (candidate, source, evidence);
    }
}
