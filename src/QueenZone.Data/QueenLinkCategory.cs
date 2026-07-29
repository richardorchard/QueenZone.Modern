namespace QueenZone.Data;

public sealed record QueenLinkCategory(
    int Id,
    string Name,
    IReadOnlyList<QueenLink> Links);
