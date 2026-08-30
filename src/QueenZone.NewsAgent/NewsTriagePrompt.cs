using System.Text;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsTriagePrompt
{
    public const string Version = "triage-v2";

    /// <summary>
    /// Non-negotiable output contract and safety rules. Always present in the final
    /// system prompt, before the editorial guidance section, and never overridable
    /// from the admin-editable overlay.
    /// </summary>
    public static string BuildFixedContract() => """
        Respond with JSON only. Do not include markdown fences or prose outside the JSON object.

        Allowed verdict values:
        - relevant
        - maybe_relevant
        - duplicate
        - not_relevant
        - unsafe_blocked

        Score relevance and confidence from 0 to 1.

        Mark unsafe_blocked for rumors without credible sourcing, private or leaked material, or stories with no QueenZone editorial value.
        Mark duplicate when the item appears to repeat an already discovered story rather than a genuinely new development.
        Require stronger evidence for lower-confidence secondary sources.
        """;

    /// <summary>
    /// The default editorial guidance: what counts as relevant, categories, entities.
    /// This is the text shown pre-filled in the admin prompt-settings editor, and it is
    /// what the compiled prompt uses when no admin overlay has been published. Admins can
    /// fully replace it via the guidance overlay without touching <see cref="BuildFixedContract"/>.
    /// </summary>
    public static string BuildDefaultEditorialGuidance() => """
        You triage discovered news items for QueenZone, a Queen band archive and news site.
        Suggested categories include tour, release, exhibition, auction, anniversary, archival, charity, award, documentary, book, member-news, other.
        Extract normalized entities when present: Queen, Freddie Mercury, Brian May, Roger Taylor, John Deacon, Adam Lambert, tours, releases, exhibitions, auctions, anniversaries, archival content.

        Preservation policy:
        - QueenZone preserves substantive news about Queen and its current or former band members: Freddie Mercury, Brian May, Roger Taylor, and John Deacon.
        - Treat a story as relevant when a Queen member is an active subject, participant, performer, creator, collaborator, or substantial topic, even when the main project belongs to another artist and is not a Queen project.
        - Treat interviews and retrospectives as relevant when they contain substantive, attributable comments or anecdotes about Queen or a Queen member. This includes another artist discussing working with, performing with, or being influenced by a Queen member.
        - For example, an article about Tony Iommi's solo album that discusses Brian May's guest performance or quotes Iommi talking about May is member-news and should be preserved for review.
        - Do not reject an item merely because it concerns a member's solo work, guest appearance, collaboration, personal creative work, or comments made by someone outside Queen.
        - A passing name-check, unrelated keyword match, scraped tag page, or story with no substantive Queen connection is not enough.
        - When a substantive Queen-member connection exists, return relevant (or maybe_relevant only when the source or connection is genuinely uncertain) with scores high enough for human review. Explain the connection in review_notes.
        """;

    public static string BuildCompiledSystemPrompt() =>
        BuildFixedContract() + "\n\n" + BuildDefaultEditorialGuidance();

    public static string ComposeSystemPrompt(string? editorialGuidance = null) =>
        string.IsNullOrWhiteSpace(editorialGuidance)
            ? BuildCompiledSystemPrompt()
            : NewsAgentEditorialGuidance.AppendToSystemPrompt(BuildFixedContract(), editorialGuidance);

    public static IReadOnlyList<NewsAiChatMessage> BuildMessages(
        NewsCandidate candidate,
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence,
        string? editorialGuidance = null)
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

        var systemPrompt = ComposeSystemPrompt(editorialGuidance);

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
