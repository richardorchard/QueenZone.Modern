namespace QueenZone.Data;

public interface IHomePollRepository
{
    /// <summary>
    /// The single live Home poll (<c>IsCurrent = true</c>), with options and vote aggregates.
    /// Returns <see langword="null"/> when none is current — callers omit the Home block.
    /// </summary>
    Task<HomePollResults?> GetCurrentAsync(
        Guid? viewerMemberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HomePollAdminItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<HomePollAdminDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        AdminHomePollDraft draft,
        Guid createdByMemberId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, AdminHomePollDraft draft, CancellationToken cancellationToken = default);

    Task PublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task CloseAsync(Guid id, CancellationToken cancellationToken = default);

    Task HideAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task CastVoteAsync(Guid optionId, Guid memberId, CancellationToken cancellationToken = default);
}
