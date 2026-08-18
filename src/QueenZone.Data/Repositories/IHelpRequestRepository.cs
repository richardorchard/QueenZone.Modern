namespace QueenZone.Data;

public interface IHelpRequestRepository
{
    Task<HelpRequest> CreateAsync(HelpRequest request, CancellationToken cancellationToken = default);

    Task<HelpRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HelpRequestListPage> ListAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<HelpRequest?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<int> CountByEmailSinceAsync(
        string normalizedEmail,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountByMemberSinceAsync(
        Guid memberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);
}
