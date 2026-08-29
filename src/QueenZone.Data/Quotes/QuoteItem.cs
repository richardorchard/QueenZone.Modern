namespace QueenZone.Data;

public sealed record QuoteItem(
    int Id,
    string Text,
    string WhoSaid,
    DateTime CreatedAt,
    bool IsPublished,
    string? Context = null);
