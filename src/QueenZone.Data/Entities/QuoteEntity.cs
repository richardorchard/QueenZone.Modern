namespace QueenZone.Data.Entities;

public sealed class QuoteEntity
{
    public int QuoteId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string WhoSaid { get; set; } = string.Empty;

    public string? Context { get; set; }

    public QuoteSourceType? SourceType { get; set; }

    public string? SourceKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsPublished { get; set; }
}
