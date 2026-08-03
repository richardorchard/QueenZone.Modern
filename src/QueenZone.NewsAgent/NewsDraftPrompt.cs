using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsDraftPrompt
{
    public const string Version = "draft-v2";

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
            Do not copy full articles, long passages, headlines, or copyrighted narrative from sources.
            Keep the body concise (roughly 2-5 short paragraphs).
            Preserve only short, newsworthy quotations.

            Quote policy (mandatory):
            Preserve short, newsworthy direct quotations from Queen band members exactly as supplied in the evidence. Never invent a quote, alter quoted wording, combine fragments, or place a paraphrase inside quotation marks. Identify the speaker and source. If exact wording is unavailable, paraphrase without quotation marks. Quote band members selectively; do not copy the source article’s surrounding prose.
            "Band members" includes Queen, Freddie Mercury, Brian May, Roger Taylor, and John Deacon. Adam Lambert or another named collaborator may be preserved when directly relevant, but must be identified accurately rather than described as a Queen member.
            Every quoted passage you place in the body must also appear in preserved_quotes.exact_text and must be taken from the supplied evidence. If a quote cannot be verified exactly from the evidence, omit quotation marks and paraphrase it.

            Every draft must include source attribution:
            - at least one source URL
            - at least one source name
            - attribution_text summarizing sources
            - source_notes with provenance
            - confidence_notes for editors
            - preserved_quotes array (may be empty) with speaker, exact_text, source_url, optional source_context

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
            attribution_text, confidence_notes, source_notes, suggested_publish_at,
            secondary_source_warning, preserved_quotes
            """;

        return
        [
            new NewsAiChatMessage("system", systemPrompt),
            new NewsAiChatMessage("user", userPrompt.Trim())
        ];
    }
}
