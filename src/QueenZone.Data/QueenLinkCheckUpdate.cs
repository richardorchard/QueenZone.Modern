namespace QueenZone.Data;

public sealed record QueenLinkCheckUpdate(
    int QueenFeaturedSiteId,
    string Url,
    DateTime CheckedAtUtc,
    bool IsAvailable,
    bool IsConfirmedDead,
    int ConsecutiveFailureCount,
    int? LastStatusCode,
    string? LastError);
