namespace QueenZone.Data;

public static class NewsDiscoveryPromoteDraft
{
    public static AdminNewsDraft Build(NewsAgentDraft agentDraft, NewsCandidate candidate)
    {
        var body = agentDraft.ProposedBody;
        if (!string.IsNullOrWhiteSpace(agentDraft.AttributionText))
        {
            body = $"{body.TrimEnd()}\n\n{agentDraft.AttributionText.Trim()}";
        }

        return new AdminNewsDraft(
            agentDraft.ProposedTitle,
            agentDraft.ProposedSlug,
            agentDraft.ProposedExcerpt,
            body,
            agentDraft.SuggestedPublishAt ?? DateTime.UtcNow.Date,
            candidate.SourceUrl);
    }
}
