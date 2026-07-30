namespace QueenZone.Data;

public sealed record AdminBiographyDraft(
    string Title,
    string Summary,
    string Body,
    byte DisplaySequence);
