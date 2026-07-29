namespace QueenZone.Data;

public sealed record QueenLinkValidationItem(
    QueenLink Link,
    int ConsecutiveFailureCount,
    bool IsConfirmedDead);
