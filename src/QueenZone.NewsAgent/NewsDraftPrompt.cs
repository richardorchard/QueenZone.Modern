using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsDraftPrompt
{
    public const string Version = "draft-v4";

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

            Write from the perspective of a knowledgeable Queen fan who is familiar with the band's music, history, solo careers, and collaborators. Use that familiarity to add concise, relevant context, while remaining factual and archive-friendly. Do not write in the first person, use fan-club hype, assume facts not present in the evidence, or overstate the Queen connection.
            Produce original summary/reporting based on the supplied source material.
            Do not copy full articles, long passages, headlines, or copyrighted narrative from sources.
            Keep the body concise (roughly 2-5 short paragraphs).
            Preserve only short, newsworthy quotations.

            Media link policy (mandatory):
            When the evidence supplies a direct song, audio, streaming, or video URL relevant to the story, include it once in the body near the relevant sentence so readers can listen or watch. Use a short descriptive HTML link such as <a href="EXACT_EVIDENCE_URL">Listen to the song</a> or <a href="EXACT_EVIDENCE_URL">Watch the video</a>. Use only the exact http or https URL supplied in the evidence. Never invent, guess, rewrite, or shorten a URL. Do not emit iframe, script, audio, video, or embed markup. If the evidence supplies no direct media URL, do not add one.

            Quote policy (mandatory):
            Preserve short, newsworthy direct quotations about Queen or a Queen band member exactly as supplied in the evidence, including relevant comments made by other artists, collaborators, producers, friends, or interviewees. When such a quote is central to the story, prefer including at least one useful short quote rather than reducing every comment to paraphrase.
            Also preserve relevant direct quotations from Queen band members. "Band members" includes Freddie Mercury, Brian May, Roger Taylor, and John Deacon. Adam Lambert or another named collaborator may be quoted when directly relevant, but must be identified accurately rather than described as a Queen member.
            For example, in a story about Tony Iommi's album and Brian May's guest performance, preserve a short evidence-backed quote from Iommi about May and explain the musical context for Queen fans.
            Never invent a quote, alter quoted wording, combine fragments, or place a paraphrase inside quotation marks. Identify the speaker and source. If exact wording is unavailable, paraphrase without quotation marks. Quote selectively; do not copy the source article's surrounding prose.
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

            The body may contain safe paragraph and anchor HTML only as required by the media link policy.
            """;

        return
        [
            new NewsAiChatMessage("system", systemPrompt),
            new NewsAiChatMessage("user", userPrompt.Trim())
        ];
    }
}
