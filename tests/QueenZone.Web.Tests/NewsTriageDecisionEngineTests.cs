using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsTriageDecisionEngineTests
{
    private static readonly NewsTriageOptions Options = new();

    [Fact]
    public void Decide_marks_deterministic_duplicate_as_ignored_duplicate()
    {
        var decision = NewsTriageDecisionEngine.Decide(
            NewsDiscoveryTrustTier.Primary,
            new NewsTriageDeterministicSignals(false, true, 12, "Earlier candidate 12 matches title hash."),
            null,
            Options);

        Assert.Equal(NewsTriageVerdict.Duplicate, decision.Verdict);
        Assert.Equal(NewsCandidateStatus.IgnoredDuplicate, decision.TargetStatus);
        Assert.Equal(12, decision.DuplicateOfCandidateId);
        Assert.True(decision.DeterministicOnly);
    }

    [Fact]
    public void Decide_rejects_secondary_source_without_keyword_match_when_ai_disabled()
    {
        var decision = NewsTriageDecisionEngine.Decide(
            NewsDiscoveryTrustTier.Secondary,
            new NewsTriageDeterministicSignals(false, true, null, null),
            null,
            Options);

        Assert.Equal(NewsCandidateStatus.Rejected, decision.TargetStatus);
        Assert.True(decision.DeterministicOnly);
    }

    [Fact]
    public void Decide_promotes_relevant_primary_candidate_when_thresholds_met()
    {
        var decision = NewsTriageDecisionEngine.Decide(
            NewsDiscoveryTrustTier.Primary,
            new NewsTriageDeterministicSignals(true, true, null, null),
            new NewsTriageStructuredResult(
                NewsTriageVerdict.Relevant,
                0.91m,
                0.87m,
                "Official Queen box set announcement.",
                "release",
                ["Queen", "release"],
                "High confidence."),
            Options);

        Assert.Equal(NewsCandidateStatus.NeedsReview, decision.TargetStatus);
        Assert.True(decision.UsedAi);
    }

    [Fact]
    public void Decide_rejects_maybe_relevant_item_below_secondary_threshold()
    {
        var decision = NewsTriageDecisionEngine.Decide(
            NewsDiscoveryTrustTier.Secondary,
            new NewsTriageDeterministicSignals(true, true, null, null),
            new NewsTriageStructuredResult(
                NewsTriageVerdict.MaybeRelevant,
                0.70m,
                0.72m,
                "Mentions Queen briefly.",
                "other",
                ["Queen"],
                null),
            Options);

        Assert.Equal(NewsCandidateStatus.Rejected, decision.TargetStatus);
    }
}
