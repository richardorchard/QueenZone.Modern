namespace QueenZone.Web;

public sealed record ContactTopicDto(string Value, string Label);

public sealed record ContactFieldLimitsDto(
    int MinSubjectLength,
    int MaxSubjectLength,
    int MinMessageLength,
    int MaxMessageLength,
    int MaxNameLength,
    int MaxEmailLength);

public sealed record ContactFormDto(
    bool SignedIn,
    string? SignedInDisplayName,
    bool RequiresContactDetails,
    string FormStamp,
    string Intro,
    string ConfirmationTitle,
    string ConfirmationMessage,
    IReadOnlyList<ContactTopicDto> Topics,
    ContactFieldLimitsDto Limits);

public sealed record ContactSubmitRequest(
    string? Topic,
    string? Subject,
    string? Message,
    string? Name,
    string? Email,
    string? Website,
    string? FormStamp);

public sealed record ContactSubmitResponse(
    bool Submitted,
    string ConfirmationTitle,
    string ConfirmationMessage);
