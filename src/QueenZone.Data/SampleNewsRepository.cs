namespace QueenZone.Data;

public sealed class SampleNewsRepository : INewsRepository
{
    private static readonly IReadOnlyList<NewsItem> Items =
    [
        new(
            1003,
            "QueenZone modernisation begins",
            "The first local vertical slice is now running from the new ASP.NET Core application.",
            "This placeholder item keeps the local site useful until a restored legacy SQL Server connection is configured.",
            new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            null,
            true),
        new(
            1002,
            "Legacy news archive mapped",
            "The first migration target is NEWS_T, with clean canonical routes for the modern archive.",
            "News gives the rebuild a narrow, valuable slice through data access, routing, page rendering, search-friendly URLs, and deployment.",
            new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            null,
            true),
        new(
            1001,
            "Read-only launch strategy accepted",
            "The first public release will focus on preserving archive content before restoring interactive features.",
            "Accounts, posting, uploads, private messages, and administration are intentionally out of scope for the first release.",
            new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc),
            null,
            true)
    ];

    public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsItem>>(Items.Take(count).ToList());

    public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<NewsItem>>(Items.Skip(skip).Take(pageSize).ToList());
    }

    public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
}
