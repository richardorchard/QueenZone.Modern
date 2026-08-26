namespace QueenZone.Data;

public interface INewsAgentGuidanceRepository
{
    Task<NewsAgentGuidanceRevision?> GetPublishedAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken = default);

    Task<NewsAgentGuidanceRevision?> GetDraftAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken = default);

    Task<NewsAgentGuidanceRevision?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsAgentGuidanceRevision>> ListHistoryAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken = default);

    Task<NewsAgentGuidanceRevision> SaveDraftAsync(
        NewsAgentGuidanceType type,
        string content,
        string editorEmail,
        byte[]? expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<NewsAgentGuidanceRevision> PublishDraftAsync(
        NewsAgentGuidanceType type,
        string publisherEmail,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<NewsAgentGuidanceRevision> RollbackAsync(
        NewsAgentGuidanceType type,
        int sourceRevisionId,
        string publisherEmail,
        CancellationToken cancellationToken = default);

    Task<NewsAgentGuidanceRevision> RestoreCompiledDefaultAsync(
        NewsAgentGuidanceType type,
        string publisherEmail,
        CancellationToken cancellationToken = default);
}
