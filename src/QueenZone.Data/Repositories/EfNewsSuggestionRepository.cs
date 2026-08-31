using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfNewsSuggestionRepository(QueenZoneDbContext dbContext) : INewsSuggestionRepository
{
    public async Task<NewsSuggestion> CreateAsync(
        NewsSuggestion suggestion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        var entity = new NewsSuggestionEntity
        {
            Id = suggestion.Id == Guid.Empty ? Guid.NewGuid() : suggestion.Id,
            SubmitterMemberId = suggestion.SubmitterMemberId,
            Url = suggestion.Url.Trim(),
            UrlHash = suggestion.UrlHash,
            Title = NormalizeOptional(suggestion.Title, 300),
            Notes = NormalizeOptional(suggestion.Notes, 1000),
            Status = NewsSuggestionStatus.Pending,
            SubmittedAt = suggestion.SubmittedAt == default ? DateTimeOffset.UtcNow : suggestion.SubmittedAt,
        };

        dbContext.NewsSuggestions.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsActiveUrlHashUniqueViolation(ex))
        {
            throw new DuplicateActiveNewsSuggestionException(ex);
        }

        return Map(entity);
    }

    public async Task<IReadOnlyList<NewsSuggestionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // SQLite EF provider cannot translate DateTimeOffset ORDER BY or navigation joins
        // inside paginated queries; fall back to materialise-then-page in C# for tests.
        // On SQL Server the full query runs in a single round-trip.
        if (IsSqliteDatabase())
        {
            var allRows = await dbContext.NewsSuggestions
                .AsNoTracking()
                .Where(row =>
                    row.Status == NewsSuggestionStatus.Pending
                    || row.Status == NewsSuggestionStatus.UnderReview)
                .Select(row => new
                {
                    row.Id,
                    row.Url,
                    row.Title,
                    DisplayName = row.Submitter != null ? row.Submitter.DisplayName : string.Empty,
                    row.SubmittedAt,
                    row.Status,
                })
                .ToListAsync(cancellationToken);

            return allRows
                .OrderByDescending(row => row.SubmittedAt)
                .ThenBy(row => row.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new NewsSuggestionListItem(
                    row.Id,
                    row.Url,
                    row.Title,
                    string.IsNullOrWhiteSpace(row.DisplayName) ? "Unknown member" : row.DisplayName,
                    row.SubmittedAt,
                    row.Status))
                .ToList();
        }

        return await PendingQueueQuery((page - 1) * pageSize, pageSize).ToListAsync(cancellationToken);
    }

    public async Task<NewsSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Include(row => row.Submitter)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<NewsSubmissionAttribution>> GetPromotedAttributionsAsync(
        IReadOnlyCollection<int> newsIds,
        CancellationToken cancellationToken = default)
    {
        if (newsIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(row => row.Status == NewsSuggestionStatus.Promoted
                && row.PromotedNewsId != null
                && newsIds.Contains(row.PromotedNewsId.Value)
                && row.Submitter != null)
            .Select(row => new NewsSubmissionAttribution(
                row.PromotedNewsId!.Value,
                row.SubmitterMemberId,
                row.Submitter!.DisplayName))
            .ToListAsync(cancellationToken);

        return ResolveUnambiguousAttributions(rows);
    }

    public async Task<SubmissionListPage<NewsSuggestion>> GetBySubmitterAsync(
        Guid submitterMemberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(row => row.SubmitterMemberId == submitterMemberId);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        if (IsSqliteDatabase())
        {
            var sqliteRows = await query
                .Select(row => new
                {
                    row.Id,
                    row.SubmitterMemberId,
                    row.Url,
                    row.UrlHash,
                    row.Title,
                    row.Notes,
                    row.Status,
                    row.SubmittedAt,
                    row.ReviewedAt,
                    row.ReviewerEmail,
                    row.ReviewNotes,
                    row.PromotedNewsId,
                    row.DuplicateCandidateId,
                    DisplayName = row.Submitter != null ? row.Submitter.DisplayName : null,
                    Email = row.Submitter != null ? row.Submitter.Email : null,
                })
                .ToListAsync(cancellationToken);

            var sqliteItems = sqliteRows
                .OrderByDescending(row => row.SubmittedAt)
                .ThenBy(row => row.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(row => new NewsSuggestion(
                    row.Id,
                    row.SubmitterMemberId,
                    row.Url,
                    row.UrlHash,
                    row.Title,
                    row.Notes,
                    row.Status,
                    row.SubmittedAt,
                    row.ReviewedAt,
                    row.ReviewerEmail,
                    row.ReviewNotes,
                    row.PromotedNewsId,
                    row.DuplicateCandidateId,
                    row.DisplayName,
                    row.Email))
                .ToList();
            return new SubmissionListPage<NewsSuggestion>(sqliteItems, totalCount);
        }

        var items = await MemberQueueQuery(submitterMemberId, skip, pageSize).ToListAsync(cancellationToken);
        return new SubmissionListPage<NewsSuggestion>(items, totalCount);
    }

    public async Task<NewsSuggestion?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.NewsSuggestions
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = NewsSuggestionStatus.Normalize(status);
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
        entity.ReviewNotes = NormalizeOptional(notes, 500);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> HasActiveDuplicateAsync(string urlHash, CancellationToken cancellationToken = default)
    {
        return await dbContext.NewsSuggestions
            .AsNoTracking()
            .AnyAsync(
                row => row.UrlHash == urlHash
                    && (row.Status == NewsSuggestionStatus.Pending
                        || row.Status == NewsSuggestionStatus.UnderReview),
                cancellationToken);
    }

    public async Task<int> CountBySubmitterSinceAsync(
        Guid submitterMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(row => row.SubmitterMemberId == submitterMemberId)
            .Select(row => row.SubmittedAt)
            .ToListAsync(cancellationToken);

        return rows.Count(submittedAt => submittedAt >= sinceUtc);
    }

    public async Task<NewsSuggestion?> PromoteAsync(
        Guid id,
        int promotedNewsId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.NewsSuggestions
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = NewsSuggestionStatus.Promoted;
        entity.PromotedNewsId = promotedNewsId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
        entity.ReviewNotes = NormalizeOptional(reviewNotes, 500);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<NewsSuggestion?> MarkDuplicateAsync(
        Guid id,
        int duplicateCandidateId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.NewsSuggestions
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = NewsSuggestionStatus.Duplicate;
        entity.DuplicateCandidateId = duplicateCandidateId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
        entity.ReviewNotes = NormalizeOptional(reviewNotes, 500);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public Task<SubmissionTypeCounts> GetDashboardCountsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default) =>
        IsSqliteDatabase()
            ? GetDashboardCountsInMemoryAsync(utcNow, cancellationToken)
            : GetDashboardCountsViaSqlAggregateAsync(utcNow, cancellationToken);

    // SQLite fallback (also exercised in tests): the provider cannot translate DateTimeOffset
    // comparisons inside conditional aggregates, so materialise then count in memory.
    private async Task<SubmissionTypeCounts> GetDashboardCountsInMemoryAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var monthAgo = utcNow.AddDays(-30);
        var today = utcNow.UtcDateTime.Date;
        var weekAgo = today.AddDays(-6);

        var rows = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Select(r => new { r.Status, r.SubmittedAt })
            .ToListAsync(cancellationToken);

        var pending = rows.Count(r =>
            r.Status is NewsSuggestionStatus.Pending or NewsSuggestionStatus.UnderReview);

        var receivedToday = rows.Count(r => r.SubmittedAt.UtcDateTime.Date >= today);
        var receivedThisWeek = rows.Count(r => r.SubmittedAt.UtcDateTime.Date >= weekAgo);

        var last30 = rows.Where(r => r.SubmittedAt >= monthAgo).ToList();
        var approvedLast30 = last30.Count(r => r.Status == NewsSuggestionStatus.Promoted);
        var rejectedLast30 = last30.Count(r =>
            r.Status is NewsSuggestionStatus.Rejected or NewsSuggestionStatus.Duplicate);
        var pendingLast30 = last30.Count(r =>
            r.Status is NewsSuggestionStatus.Pending or NewsSuggestionStatus.UnderReview);

        return new SubmissionTypeCounts(
            pending, receivedToday, receivedThisWeek, approvedLast30, rejectedLast30, pendingLast30);
    }

    // SQL Server only: the EF Core SQLite provider cannot translate DateTimeOffset comparisons
    // inside conditional aggregates, so this path has no coverage from the default SQLite-backed
    // QueenZone.Web.Tests suite. Covered instead by tests/QueenZone.SqlServerTests against a
    // real SQL Server (Docker in CI, LocalDB locally) — see docs/architecture/testing-policy.md.
    private async Task<SubmissionTypeCounts> GetDashboardCountsViaSqlAggregateAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var monthAgo = utcNow.AddDays(-30);
        var todayUtc = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var weekAgoUtc = todayUtc.AddDays(-6);

        var counts = await dbContext.NewsSuggestions
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new SubmissionTypeCounts(
                g.Count(r => r.Status == NewsSuggestionStatus.Pending || r.Status == NewsSuggestionStatus.UnderReview),
                g.Count(r => r.SubmittedAt >= todayUtc),
                g.Count(r => r.SubmittedAt >= weekAgoUtc),
                g.Count(r => r.SubmittedAt >= monthAgo && r.Status == NewsSuggestionStatus.Promoted),
                g.Count(r => r.SubmittedAt >= monthAgo
                    && (r.Status == NewsSuggestionStatus.Rejected || r.Status == NewsSuggestionStatus.Duplicate)),
                g.Count(r => r.SubmittedAt >= monthAgo
                    && (r.Status == NewsSuggestionStatus.Pending || r.Status == NewsSuggestionStatus.UnderReview))))
            .SingleOrDefaultAsync(cancellationToken);

        return counts ?? SubmissionTypeCounts.Empty;
    }

    public Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsThisMonthAsync(
        DateTimeOffset monthStart,
        int maxCount,
        CancellationToken cancellationToken = default) =>
        IsSqliteDatabase()
            ? GetTopContributorsInMemoryAsync(monthStart, maxCount, cancellationToken)
            : GetTopContributorsViaSqlAggregateAsync(monthStart, maxCount, cancellationToken);

    // SQLite fallback (also exercised in tests): the provider cannot translate DateTimeOffset
    // comparisons.
    private async Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsInMemoryAsync(
        DateTimeOffset monthStart,
        int maxCount,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Select(r => new
            {
                r.SubmitterMemberId,
                DisplayName = r.Submitter != null ? r.Submitter.DisplayName : string.Empty,
                r.SubmittedAt,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => r.SubmittedAt >= monthStart)
            .GroupBy(r => r.SubmitterMemberId)
            .Select(g => new SubmissionContributor(
                g.Key,
                g.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.DisplayName))?.DisplayName ?? "Unknown member",
                g.Count()))
            .OrderByDescending(c => c.Count)
            .Take(maxCount)
            .ToList();
    }

    // SQL Server only: see the note on GetDashboardCountsViaSqlAggregateAsync.
    private async Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsViaSqlAggregateAsync(
        DateTimeOffset monthStart,
        int maxCount,
        CancellationToken cancellationToken)
    {
        var aggregated = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(r => r.SubmittedAt >= monthStart)
            .GroupBy(r => r.SubmitterMemberId)
            .Select(g => new
            {
                SubmitterMemberId = g.Key,
                DisplayName = g.Max(r => r.Submitter != null ? r.Submitter.DisplayName : null),
                Count = g.Count(),
            })
            .OrderByDescending(c => c.Count)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        return aggregated
            .Select(c => new SubmissionContributor(
                c.SubmitterMemberId,
                string.IsNullOrWhiteSpace(c.DisplayName) ? "Unknown member" : c.DisplayName,
                c.Count))
            .ToList();
    }

    internal const string ActiveUrlHashIndexName = "IX_NewsSuggestions_UrlHash_Active";

    internal static bool IsActiveUrlHashUniqueViolation(DbUpdateException exception)
    {
        var sawSqlUnique = false;
        var sawIndexName = false;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 2601 or 2627)
            {
                sawSqlUnique = true;
            }

            if (current.Message.Contains(ActiveUrlHashIndexName, StringComparison.Ordinal))
            {
                sawIndexName = true;
            }
        }

        return sawSqlUnique && sawIndexName;
    }

    private bool IsSqliteDatabase() =>
        string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    internal IQueryable<NewsSuggestionListItem> PendingQueueQuery(int skip, int take) =>
        dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(row =>
                row.Status == NewsSuggestionStatus.Pending
                || row.Status == NewsSuggestionStatus.UnderReview)
            .OrderByDescending(row => row.SubmittedAt)
            .ThenBy(row => row.Id)
            .Skip(skip)
            .Take(take)
            .Select(row => new NewsSuggestionListItem(
                row.Id,
                row.Url,
                row.Title,
                row.Submitter != null ? row.Submitter.DisplayName : "Unknown member",
                row.SubmittedAt,
                row.Status));

    internal IQueryable<NewsSuggestion> MemberQueueQuery(Guid submitterMemberId, int skip, int take) =>
        dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(row => row.SubmitterMemberId == submitterMemberId)
            .OrderByDescending(row => row.SubmittedAt)
            .ThenBy(row => row.Id)
            .Skip(skip)
            .Take(take)
            .Select(row => new NewsSuggestion(
                row.Id,
                row.SubmitterMemberId,
                row.Url,
                row.UrlHash,
                row.Title,
                row.Notes,
                row.Status,
                row.SubmittedAt,
                row.ReviewedAt,
                row.ReviewerEmail,
                row.ReviewNotes,
                row.PromotedNewsId,
                row.DuplicateCandidateId,
                row.Submitter != null ? row.Submitter.DisplayName : null,
                row.Submitter != null ? row.Submitter.Email : null));

    private static NewsSuggestion Map(NewsSuggestionEntity entity) =>
        new(
            entity.Id,
            entity.SubmitterMemberId,
            entity.Url,
            entity.UrlHash,
            entity.Title,
            entity.Notes,
            entity.Status,
            entity.SubmittedAt,
            entity.ReviewedAt,
            entity.ReviewerEmail,
            entity.ReviewNotes,
            entity.PromotedNewsId,
            entity.DuplicateCandidateId,
            entity.Submitter?.DisplayName,
            entity.Submitter?.Email);

    internal static IReadOnlyList<NewsSubmissionAttribution> ResolveUnambiguousAttributions(
        IEnumerable<NewsSubmissionAttribution> rows) =>
        rows.GroupBy(row => row.NewsId)
            .Where(group => group.Select(row => row.MemberId).Distinct().Count() == 1)
            .Select(group => group.First())
            .ToList();
}
