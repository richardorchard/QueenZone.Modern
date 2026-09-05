namespace QueenZone.Data;

public sealed record FanPerformance(
    int Id,
    string Title,
    string PerformedBy,
    string Description,
    string AudioFileName,
    long FileSizeBytes,
    DateTime DateAdded,
    int? DurationSeconds = null,
    Guid? ContributorMemberId = null,
    string? ContributorDisplayName = null);
