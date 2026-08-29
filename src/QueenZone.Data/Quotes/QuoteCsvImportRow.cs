namespace QueenZone.Data;

public sealed record QuoteCsvImportRow(
    string Text,
    string WhoSaid,
    string? Context,
    QuoteSourceType SourceType,
    string SourceKey);
