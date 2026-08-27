namespace QueenZone.Data.Entities;

public sealed class QuoteEntity
{
    public int QuoteId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string WhoSaid { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsPublished { get; set; }
}
