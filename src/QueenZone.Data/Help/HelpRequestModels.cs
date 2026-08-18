namespace QueenZone.Data;

public sealed record HelpRequest(
    Guid Id,
    string Topic,
    string Subject,
    string Message,
    string Name,
    string Email,
    string NormalizedEmail,
    Guid? MemberId,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerEmail,
    string? ReviewNotes);

public sealed record HelpRequestListItem(
    Guid Id,
    string Topic,
    string Subject,
    string Name,
    string Email,
    Guid? MemberId,
    string Status,
    DateTimeOffset SubmittedAt);

public sealed record HelpRequestListPage(
    IReadOnlyList<HelpRequestListItem> Items,
    int TotalCount,
    string? StatusFilter);
