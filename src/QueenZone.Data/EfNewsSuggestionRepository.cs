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
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<NewsSuggestionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Where(row =>
                row.Status == NewsSuggestionStatus.Pending
                || row.Status == NewsSuggestionStatus.UnderReview)
            .Select(row => new
            {
                row.Id,
                row.Url,
                row.Title,
                row.SubmitterMemberId,
                DisplayName = row.Submitter != null ? row.Submitter.DisplayName : string.Empty,
                row.SubmittedAt,
                row.Status,
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.SubmittedAt)
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

    public async Task<NewsSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.NewsSuggestions
            .AsNoTracking()
            .Include(row => row.Submitter)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
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

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

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
}
