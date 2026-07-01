using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsDraftPrompt
{
    public const string Version = "draft-v1";

    public static IReadOnlyList<NewsAiChatMessage> BuildMessages(
        NewsCandidate candidate,
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var evidenceSummary = string.Join(
            Environment.NewLine,
            evidence.Select(item =>
                $"- {item.SourceName} ({item.SourceTrustTier}): {item.FetchedTitle}\n  URL: {item.CanonicalUrl}\n  Excerpt: {item.Excerpt}"));

        var systemPrompt = """
            You draft QueenZone news articles for editor review.
            Respond with JSON only. Do not include markdown fences or prose outside the JSON object.

            Write in QueenZone's clear archive-friendly editorial voice.
            Produce original summary/reporting based on the supplied source material.
            Do not copy full articles, long passages, or copyrighted text from sources.
            Keep the body concise (roughly 2-5 short paragraphs).

            Every draft must include source attribution:
            - at least one source URL
            - at least one source name
            - attribution_text summarizing sources
            - source_notes with provenance
            - confidence_notes for editors

            Flag secondary_source_warning=true when the story relies only on secondary press or weaker evidence.
            """;

        var userPrompt = $"""
            Candidate id: {candidate.Id}
            Source key: {source.Key}
            Source name: {source.DisplayName}
            Trust tier: {source.TrustTier}
            Candidate URL: {candidate.SourceUrl}
            Canonical URL: {candidate.CanonicalUrl}
            Source title: {candidate.SourceTitle}
            Published at: {candidate.SourcePublishedAt:O}
            Relevance score: {candidate.RelevanceScore}
            Confidence score: {candidate.ConfidenceScore}

            Evidence:
            {evidenceSummary}

            Return JSON with:
            title, slug, excerpt, body, related_entities, source_urls, source_names,
            attribution_text, confidence_notes, source_notes, suggested_publish_at, secondary_source_warning
            """;

        return
        [
            new NewsAiChatMessage("system", systemPrompt),
            new NewsAiChatMessage("user", userPrompt.Trim())
        ];
    }
}
