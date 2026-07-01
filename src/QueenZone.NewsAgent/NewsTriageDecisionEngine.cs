using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsTriageDecisionEngine
{
    public static NewsTriageDecision Decide(
        NewsDiscoveryTrustTier trustTier,
        NewsTriageDeterministicSignals deterministic,
        NewsTriageStructuredResult? ai,
        NewsTriageOptions options)
    {
        if (deterministic.DuplicateOfCandidateId is not null)
        {
            return new NewsTriageDecision(
                NewsTriageVerdict.Duplicate,
                NewsCandidateStatus.IgnoredDuplicate,
                0m,
                1m,
                deterministic.DuplicateReason ?? "Deterministic duplicate match.",
                deterministic.DuplicateOfCandidateId,
                UsedAi: false,
                DeterministicOnly: true);
        }

        if (ai is null)
        {
            if (trustTier == NewsDiscoveryTrustTier.Secondary && !deterministic.KeywordMatch)
            {
                return new NewsTriageDecision(
                    NewsTriageVerdict.NotRelevant,
                    NewsCandidateStatus.Rejected,
                    0m,
                    0.9m,
                    "Secondary source item did not match configured relevance keywords.",
                    null,
                    UsedAi: false,
                    DeterministicOnly: true);
            }

            return new NewsTriageDecision(
                NewsTriageVerdict.MaybeRelevant,
                NewsCandidateStatus.Discovered,
                0m,
                0m,
                "AI triage unavailable; candidate left in discovered state.",
                null,
                UsedAi: false,
                DeterministicOnly: true);
        }

        var relevance = ai.RelevanceScore;
        var confidence = ai.ConfidenceScore;

        if (deterministic.KeywordMatch)
        {
            relevance = Math.Max(relevance, options.MinRelevanceScore(trustTier));
        }

        if (trustTier == NewsDiscoveryTrustTier.Primary)
        {
            confidence = Math.Max(confidence, options.PrimaryMinConfidenceScore);
        }

        if (ai.Verdict == NewsTriageVerdict.MaybeRelevant)
        {
            confidence = Math.Max(0m, confidence - options.MaybeRelevantConfidencePenalty);
        }

        var reviewNotes = BuildReviewNotes(ai, deterministic);

        return ai.Verdict switch
        {
            NewsTriageVerdict.UnsafeBlocked => new NewsTriageDecision(
                ai.Verdict,
                NewsCandidateStatus.Rejected,
                relevance,
                confidence,
                reviewNotes,
                null,
                UsedAi: true,
                DeterministicOnly: false),
            NewsTriageVerdict.Duplicate => new NewsTriageDecision(
                ai.Verdict,
                NewsCandidateStatus.IgnoredDuplicate,
                relevance,
                confidence,
                reviewNotes,
                deterministic.DuplicateOfCandidateId,
                UsedAi: true,
                DeterministicOnly: false),
            NewsTriageVerdict.NotRelevant => new NewsTriageDecision(
                ai.Verdict,
                NewsCandidateStatus.Rejected,
                relevance,
                confidence,
                reviewNotes,
                null,
                UsedAi: true,
                DeterministicOnly: false),
            NewsTriageVerdict.MaybeRelevant or NewsTriageVerdict.Relevant when MeetsThresholds(
                trustTier,
                ai.Verdict,
                relevance,
                confidence,
                options) => new NewsTriageDecision(
                ai.Verdict,
                NewsCandidateStatus.NeedsReview,
                relevance,
                confidence,
                reviewNotes,
                null,
                UsedAi: true,
                DeterministicOnly: false),
            _ => new NewsTriageDecision(
                ai.Verdict,
                NewsCandidateStatus.Rejected,
                relevance,
                confidence,
                reviewNotes,
                null,
                UsedAi: true,
                DeterministicOnly: false)
        };
    }

    private static bool MeetsThresholds(
        NewsDiscoveryTrustTier trustTier,
        NewsTriageVerdict verdict,
        decimal relevance,
        decimal confidence,
        NewsTriageOptions options)
    {
        var minRelevance = options.MinRelevanceScore(trustTier);
        var minConfidence = options.MinConfidenceScore(trustTier);

        if (verdict == NewsTriageVerdict.MaybeRelevant)
        {
            minConfidence += options.MaybeRelevantConfidencePenalty;
        }

        return relevance >= minRelevance && confidence >= minConfidence;
    }

    private static string BuildReviewNotes(
        NewsTriageStructuredResult ai,
        NewsTriageDeterministicSignals deterministic)
    {
        var notes = new List<string> { ai.Rationale };

        if (!string.IsNullOrWhiteSpace(ai.SuggestedCategory))
        {
            notes.Add($"Category: {ai.SuggestedCategory}");
        }

        if (ai.Entities.Count > 0)
        {
            notes.Add($"Entities: {string.Join(", ", ai.Entities)}");
        }

        if (!string.IsNullOrWhiteSpace(ai.ReviewNotes))
        {
            notes.Add(ai.ReviewNotes);
        }

        if (deterministic.KeywordMatch)
        {
            notes.Add("Deterministic keyword match.");
        }

        if (deterministic.CanonicalUrlKnown)
        {
            notes.Add("Canonical URL present.");
        }

        return string.Join(" ", notes);
    }
}
