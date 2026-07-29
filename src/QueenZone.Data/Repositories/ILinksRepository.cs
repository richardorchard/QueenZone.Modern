namespace QueenZone.Data;

public interface ILinksRepository
{
    Task<IReadOnlyList<QueenLinkCategory>> GetCategoriesWithLinksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QueenLinkValidationItem>> GetLinksForValidationAsync(CancellationToken cancellationToken = default);

    Task UpsertCheckResultsAsync(
        IReadOnlyList<QueenLinkCheckUpdate> updates,
        CancellationToken cancellationToken = default);
}
