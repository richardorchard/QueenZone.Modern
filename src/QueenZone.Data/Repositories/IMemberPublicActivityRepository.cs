namespace QueenZone.Data;

public interface IMemberPublicActivityRepository
{
    Task<MemberPublicActivityPage> GetPageAsync(
        Guid memberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Newest-first public activity for every author in <paramref name="memberIds"/>.
    /// Same four sources and item shape as <see cref="GetPageAsync"/>.
    /// An empty id set returns no rows and does not query the sources.
    /// </summary>
    Task<MemberPublicActivityPage> GetFeedPageAsync(
        IReadOnlyCollection<Guid> memberIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
