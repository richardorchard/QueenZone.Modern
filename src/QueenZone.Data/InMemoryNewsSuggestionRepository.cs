using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryNewsSuggestionRepository : INewsSuggestionRepository
{
    private readonly object sync = new();
    private readonly List<NewsSuggestionEntity> suggestions = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;

    public InMemoryNewsSuggestionRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<NewsSuggestion> CreateAsync(
        NewsSuggestion suggestion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        lock (sync)
        {
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

            suggestions.Add(entity);
            return Task.FromResult(Map(entity));
        }
    }

    public Task<IReadOnlyList<NewsSuggestionListItem>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        lock (sync)
        {
            IReadOnlyList<NewsSuggestionListItem> result = suggestions
                .Where(row =>
                    row.Status is NewsSuggestionStatus.Pending
                        or NewsSuggestionStatus.UnderReview)
                .OrderByDescending(row => row.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row =>
                {
                    var member = resolveMember?.Invoke(row.SubmitterMemberId);
                    return new NewsSuggestionListItem(
                        row.Id,
                        row.Url,
                        row.Title,
                        member?.DisplayName ?? "Unknown member",
                        row.SubmittedAt,
                        row.Status);
                })
                .ToList();

            return Task.FromResult(result);
        }
    }

    public Task<NewsSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = suggestions.SingleOrDefault(row => row.Id == id);
            return Task.FromResult(entity is null ? null : Map(entity));
        }
    }

    public Task<NewsSuggestion?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = suggestions.SingleOrDefault(row => row.Id == id);
            if (entity is null)
            {
                return Task.FromResult<NewsSuggestion?>(null);
            }

            entity.Status = NewsSuggestionStatus.Normalize(status);
            entity.ReviewedAt = DateTimeOffset.UtcNow;
            entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
            entity.ReviewNotes = NormalizeOptional(notes, 500);

            return Task.FromResult<NewsSuggestion?>(Map(entity));
        }
    }

    public Task<bool> HasActiveDuplicateAsync(string urlHash, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var exists = suggestions.Any(row =>
                row.UrlHash == urlHash
                && (row.Status == NewsSuggestionStatus.Pending
                    || row.Status == NewsSuggestionStatus.UnderReview));
            return Task.FromResult(exists);
        }
    }

    public Task<int> CountBySubmitterSinceAsync(
        Guid submitterMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var count = suggestions.Count(row =>
                row.SubmitterMemberId == submitterMemberId && row.SubmittedAt >= sinceUtc);
            return Task.FromResult(count);
        }
    }

    public Task<NewsSuggestion?> PromoteAsync(
        Guid id,
        int promotedNewsId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = suggestions.SingleOrDefault(row => row.Id == id);
            if (entity is null)
            {
                return Task.FromResult<NewsSuggestion?>(null);
            }

            entity.Status = NewsSuggestionStatus.Promoted;
            entity.PromotedNewsId = promotedNewsId;
            entity.ReviewedAt = DateTimeOffset.UtcNow;
            entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
            entity.ReviewNotes = NormalizeOptional(reviewNotes, 500);

            return Task.FromResult<NewsSuggestion?>(Map(entity));
        }
    }

    public Task<NewsSuggestion?> MarkDuplicateAsync(
        Guid id,
        int duplicateCandidateId,
        string reviewerEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = suggestions.SingleOrDefault(row => row.Id == id);
            if (entity is null)
            {
                return Task.FromResult<NewsSuggestion?>(null);
            }

            entity.Status = NewsSuggestionStatus.Duplicate;
            entity.DuplicateCandidateId = duplicateCandidateId;
            entity.ReviewedAt = DateTimeOffset.UtcNow;
            entity.ReviewerEmail = NormalizeOptional(reviewerEmail, 256);
            entity.ReviewNotes = NormalizeOptional(reviewNotes, 500);

            return Task.FromResult<NewsSuggestion?>(Map(entity));
        }
    }

    private NewsSuggestion Map(NewsSuggestionEntity entity)
    {
        var member = resolveMember?.Invoke(entity.SubmitterMemberId);
        return new NewsSuggestion(
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
            member?.DisplayName,
            member?.Email);
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
}
