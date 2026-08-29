namespace QueenZone.Data;

public sealed record AdminQuoteDraft(
    string Text,
    string WhoSaid,
    bool IsPublished,
    string? Context = null);
