namespace QueenZone.Data;

public sealed record QueenHistoryCsvImportResult(
    int RowsRead,
    int Created,
    int Updated,
    int Unchanged);
