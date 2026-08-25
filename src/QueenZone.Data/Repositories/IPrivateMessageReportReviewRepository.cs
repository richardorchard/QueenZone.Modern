namespace QueenZone.Data;

/// <summary>
/// Narrow admin review surface for reported private messages. Every content
/// lookup is keyed by report id so this cannot grow into a general private-message
/// search or inbox browse.
/// </summary>
public interface IPrivateMessageReportReviewRepository
{
    Task<PrivateMessageReportListPage> ListReportsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the report snapshot and reporter/reported identities for
    /// <paramref name="reportId"/>. Does not query private conversations by
    /// conversation or member id.
    /// </summary>
    Task<PrivateMessageReportReviewContext?> GetReportedMessageContextAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<PrivateMessageReport?> UpdateReportStatusAsync(
        Guid reportId,
        string status,
        string actorEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an admin access or decision. Returns false when the report does
    /// not exist.
    /// </summary>
    Task<bool> RecordAccessAsync(
        Guid reportId,
        string action,
        string actorEmail,
        string? details,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);
}
