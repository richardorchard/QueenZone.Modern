namespace QueenZone.Data;

public sealed class InMemoryLinksRepository(IReadOnlyList<QueenLinkCategory> seedCategories) : ILinksRepository
{
    private readonly Dictionary<int, QueenLinkCheckUpdate> checkResults = [];

    public Task<IReadOnlyList<QueenLinkCategory>> GetCategoriesWithLinksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QueenLinkCategory>>(seedCategories
            .Select(category => category with
            {
                Links = category.Links
                    .Where(link => !checkResults.TryGetValue(link.Id, out var check) || !check.IsConfirmedDead)
                    .ToList()
            })
            .Where(category => category.Links.Count > 0)
            .ToList());

    public Task<IReadOnlyList<QueenLinkValidationItem>> GetLinksForValidationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QueenLinkValidationItem>>(seedCategories
            .SelectMany(category => category.Links)
            .Select(link => checkResults.TryGetValue(link.Id, out var check)
                ? new QueenLinkValidationItem(link, check.ConsecutiveFailureCount, check.IsConfirmedDead)
                : new QueenLinkValidationItem(link, 0, false))
            .ToList());

    public Task UpsertCheckResultsAsync(
        IReadOnlyList<QueenLinkCheckUpdate> updates,
        CancellationToken cancellationToken = default)
    {
        foreach (var update in updates)
        {
            checkResults[update.QueenFeaturedSiteId] = update;
        }

        return Task.CompletedTask;
    }
}
