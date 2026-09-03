namespace QueenZone.Data;

public sealed record TriviaFactCsvImportResult(
    int RowsRead,
    int Created,
    int Updated,
    int Unchanged);
