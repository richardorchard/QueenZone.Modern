namespace QueenZone.Data;

public sealed record NewsAgentGuidanceRevision(
    int Id,
    NewsAgentGuidanceType Type,
    int RevisionNumber,
    string Content,
    string ContentHash,
    NewsAgentGuidanceStatus Status,
    DateTime CreatedAt,
    string CreatedByEmail,
    DateTime? PublishedAt,
    string? PublishedByEmail,
    byte[] RowVersion);

public sealed class NewsAgentGuidanceConcurrencyException : InvalidOperationException
{
    public NewsAgentGuidanceConcurrencyException()
        : base("Another administrator updated this guidance draft. Reload and try again.")
    {
    }
}

public sealed class NewsAgentGuidanceValidationException : InvalidOperationException
{
    public NewsAgentGuidanceValidationException(string message)
        : base(message)
    {
    }
}
