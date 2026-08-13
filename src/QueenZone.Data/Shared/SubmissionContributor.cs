namespace QueenZone.Data;

public record SubmissionContributor(
    Guid MemberId,
    string DisplayName,
    int Count);
