using System.Text;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsTriagePrompt
{
    public const string Version = "triage-v1";

    public static IReadOnlyList<NewsAiChatMessage> BuildMessages(
        NewsCandidate candidate,
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var evidenceSummary = new StringBuilder();
        foreach (var item in evidence)
        {
            evidenceSummary.AppendLine($"- {item.SourceName}: {item.FetchedTitle}");
            if (!string.IsNullOrWhiteSpace(item.Excerpt))
            {
                evidenceSummary.AppendLine($"  Excerpt: {item.Excerpt}");
            }
        }

        var systemPrompt = """
            You triage discovered news items for QueenZone, a Queen band archive and news site.
            Respond with JSON only. Do not include markdown fences or prose outside the JSON object.

            Allowed verdict values:
            - relevant
            - maybe_relevant
            - duplicate
            - not_relevant
            - unsafe_blocked

            Score relevance and confidence from 0 to 1.
            Suggested categories include tour, release, exhibition, auction, anniversary, archival, charity, award, documentary, book, member-news, other.
            Extract normalized entities when present: Queen, Freddie Mercury, Brian May, Roger Taylor, John Deacon, Adam Lambert, tours, releases, exhibitions, auctions, anniversaries, archival content.

            Mark unsafe_blocked for rumors without credible sourcing, private or leaked material, or stories with no QueenZone editorial value.
            Mark duplicate when the item appears to repeat an already discovered story rather than a genuinely new development.
            Require stronger evidence for lower-confidence secondary sources.
            """;

        var userPrompt = $"""
            Candidate id: {candidate.Id}
            Source key: {source.Key}
            Source name: {source.DisplayName}
            Trust tier: {source.TrustTier}
            Source URL: {candidate.SourceUrl}
            Canonical URL: {candidate.CanonicalUrl}
            Title: {candidate.SourceTitle}
            Published at: {candidate.SourcePublishedAt:O}
            Configured relevance keywords: {source.RelevanceKeywords ?? "(none)"}

            Evidence:
            {evidenceSummary}

            Return JSON with:
            verdict, relevance_score, confidence_score, rationale, suggested_category, entities, review_notes
            """;

        return
        [
            new NewsAiChatMessage("system", systemPrompt),
            new NewsAiChatMessage("user", userPrompt.Trim())
        ];
    }
}
