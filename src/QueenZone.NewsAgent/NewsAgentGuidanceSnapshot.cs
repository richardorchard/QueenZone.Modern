namespace QueenZone.NewsAgent;

public sealed record NewsAgentGuidanceSnapshot(
    int? RevisionId,
    int? RevisionNumber,
    string? ContentHash,
    string Content)
{
    public static NewsAgentGuidanceSnapshot Empty { get; } = new(null, null, null, string.Empty);

    public bool HasOverlay => !string.IsNullOrWhiteSpace(Content);

    public bool HasRevision => RevisionId is not null;
}
