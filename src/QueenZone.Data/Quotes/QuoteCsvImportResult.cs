namespace QueenZone.Data;

public sealed record QuoteCsvImportResult(
    int RowsRead,
    int Created,
    int Updated,
    int Unchanged);
