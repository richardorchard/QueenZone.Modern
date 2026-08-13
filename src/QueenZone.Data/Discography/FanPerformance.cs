namespace QueenZone.Data;

public sealed record FanPerformance(
    int Id,
    string Title,
    string PerformedBy,
    string Description,
    string AudioUrl,
    long FileSizeBytes,
    DateTime DateAdded);
