using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryArticleSubmissionRepository : IArticleSubmissionRepository
{
    private readonly object sync = new();
    private readonly List<ArticleSubmissionEntity> submissions = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;

    public InMemoryArticleSubmissionRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<ArticleSubmission> UpsertDraftAsync(ArticleSubmissionDraft draft, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        lock (sync)
        {
            ArticleSubmissionEntity entity;

            if (draft.Id is { } existingId && existingId != Guid.Empty)
            {
                entity = submissions.SingleOrDefault(a => a.Id == existingId && a.AuthorMemberId == draft.AuthorMemberId)
                    ?? throw new InvalidOperationException("Article submission not found or does not belong to this member.");

                if (entity.Status != ArticleSubmissionStatus.Draft
                    && entity.Status != ArticleSubmissionStatus.RequiresRevision)
                {
                    throw new InvalidOperationException("Only draft or requires-revision submissions can be updated.");
                }

                entity.Title = Trim300(draft.Title);
                entity.Slug = GenerateSlug(draft.Title);
                entity.Excerpt = TrimOpt(draft.Excerpt, 500);
                entity.Body = draft.Body ?? string.Empty;
                entity.CoverImageBlobPath = TrimOpt(draft.CoverImageBlobPath, 512);
                entity.Tags = TrimOpt(draft.Tags, 500);
            }
            else
            {
                entity = new ArticleSubmissionEntity
                {
                    Id = Guid.NewGuid(),
                    AuthorMemberId = draft.AuthorMemberId,
                    Title = Trim300(draft.Title),
                    Slug = GenerateSlug(draft.Title),
                    Excerpt = TrimOpt(draft.Excerpt, 500),
                    Body = draft.Body ?? string.Empty,
                    CoverImageBlobPath = TrimOpt(draft.CoverImageBlobPath, 512),
                    Tags = TrimOpt(draft.Tags, 500),
                    Status = ArticleSubmissionStatus.Draft,
                };
                submissions.Add(entity);
            }

            entity.Author = resolveMember?.Invoke(entity.AuthorMemberId);
            return Task.FromResult(Map(entity));
        }
    }

    public Task<ArticleSubmission?> SubmitForReviewAsync(Guid id, Guid authorMemberId, CancellationToken ct = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(a => a.Id == id && a.AuthorMemberId == authorMemberId);

            if (entity is null)
            {
                return Task.FromResult<ArticleSubmission?>(null);
            }

            if (entity.Status != ArticleSubmissionStatus.Draft
                && entity.Status != ArticleSubmissionStatus.RequiresRevision)
            {
                return Task.FromResult<ArticleSubmission?>(null);
            }

            var visibleChars = EfArticleSubmissionRepository.CountVisibleChars(entity.Body);
            if (visibleChars < EfArticleSubmissionRepository.MinBodyVisibleChars)
            {
                throw new InvalidOperationException(
                    $"Article body must contain at least {EfArticleSubmissionRepository.MinBodyVisibleChars} characters of visible text (currently {visibleChars}).");
            }

            entity.Status = ArticleSubmissionStatus.Submitted;
            entity.SubmittedAt = DateTimeOffset.UtcNow;
            entity.Author = resolveMember?.Invoke(entity.AuthorMemberId);
            return Task.FromResult<ArticleSubmission?>(Map(entity));
        }
    }

    public Task<SubmissionListPage<ArticleSubmission>> GetDraftsForMemberAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        lock (sync)
        {
            var owned = submissions
                .Where(a => a.AuthorMemberId == memberId)
                .OrderByDescending(a => a.SubmittedAt ?? DateTimeOffset.MinValue)
                .ToList();

            var items = owned
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a =>
                {
                    a.Author = resolveMember?.Invoke(a.AuthorMemberId);
                    return Map(a);
                })
                .ToList();

            return Task.FromResult(new SubmissionListPage<ArticleSubmission>(items, owned.Count));
        }
    }

    public Task<IReadOnlyList<ArticleSubmissionListItem>> GetPendingAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        lock (sync)
        {
            var result = submissions
                .Where(a => a.Status is ArticleSubmissionStatus.Submitted
                    or ArticleSubmissionStatus.UnderReview
                    or ArticleSubmissionStatus.ApprovedForPublishing)
                .OrderByDescending(a => a.SubmittedAt ?? DateTimeOffset.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ArticleSubmissionListItem(
                    a.Id,
                    a.Title,
                    a.Status,
                    a.Author?.DisplayName ?? "Unknown member",
                    a.SubmittedAt,
                    a.PublishedAt,
                    EfArticleSubmissionRepository
                        .CountVisibleChars(a.Body)
                        / 5))
                .ToList();

            return Task.FromResult<IReadOnlyList<ArticleSubmissionListItem>>(result);
        }
    }

    public Task<ArticleSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(a => a.Id == id);
            if (entity is not null)
            {
                entity.Author = resolveMember?.Invoke(entity.AuthorMemberId);
            }

            return Task.FromResult(entity is null ? null : Map(entity));
        }
    }

    public Task<ArticleSubmission?> UpdateStatusAsync(
        Guid id,
        string status,
        string? reviewerEmail,
        string? notes,
        string? rejectionReason,
        string? slug = null,
        string? excerpt = null,
        string? tags = null,
        CancellationToken ct = default)
    {
        lock (sync)
        {
            var entity = submissions.SingleOrDefault(a => a.Id == id);
            if (entity is null)
            {
                return Task.FromResult<ArticleSubmission?>(null);
            }

            entity.Status = status;
            entity.ReviewerEmail = TrimOpt(reviewerEmail, 256);
            entity.ReviewNotes = TrimOpt(notes, 1000);

            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                entity.RejectionReason = TrimOpt(rejectionReason, 1000);
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                entity.Slug = slug.Trim();
            }

            if (excerpt is not null)
            {
                entity.Excerpt = TrimOpt(excerpt, 500);
            }

            if (tags is not null)
            {
                entity.Tags = TrimOpt(tags, 500);
            }

            if (status == ArticleSubmissionStatus.Published)
            {
                entity.PublishedAt = DateTimeOffset.UtcNow;
            }

            entity.Author = resolveMember?.Invoke(entity.AuthorMemberId);
            return Task.FromResult<ArticleSubmission?>(Map(entity));
        }
    }

    public Task<IReadOnlyList<PublishedArticleSubmission>> GetPublishedAsync(CancellationToken ct = default)
    {
        lock (sync)
        {
            var result = submissions
                .Where(a => a.Status == ArticleSubmissionStatus.Published && a.PublishedAt is not null)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => new PublishedArticleSubmission(
                    a.Id,
                    a.Title,
                    a.Slug,
                    a.Excerpt,
                    a.Body,
                    a.CoverImageBlobPath,
                    a.Tags,
                    a.PublishedAt!.Value,
                    a.Author?.DisplayName,
                    EfArticleSubmissionRepository.EstimateWordCount(a.Body)))
                .ToList();

            return Task.FromResult<IReadOnlyList<PublishedArticleSubmission>>(result);
        }
    }

    public Task<SubmissionTypeCounts> GetDashboardCountsAsync(
        DateTimeOffset utcNow,
        CancellationToken ct = default)
    {
        var today = utcNow.UtcDateTime.Date;
        var weekAgo = today.AddDays(-6);
        var monthAgo = utcNow.AddDays(-30);

        lock (sync)
        {
            var pending = submissions.Count(a =>
                a.Status is ArticleSubmissionStatus.Submitted
                    or ArticleSubmissionStatus.UnderReview
                    or ArticleSubmissionStatus.ApprovedForPublishing);

            var submitted = submissions.Where(a => a.SubmittedAt.HasValue).ToList();
            var receivedToday = submitted.Count(a => a.SubmittedAt!.Value.UtcDateTime.Date >= today);
            var receivedThisWeek = submitted.Count(a => a.SubmittedAt!.Value.UtcDateTime.Date >= weekAgo);

            var last30 = submitted.Where(a => a.SubmittedAt!.Value >= monthAgo).ToList();
            var approvedLast30 = last30.Count(a =>
                a.Status is ArticleSubmissionStatus.Published or ArticleSubmissionStatus.ApprovedForPublishing);
            var rejectedLast30 = last30.Count(a =>
                a.Status is ArticleSubmissionStatus.Rejected or ArticleSubmissionStatus.RequiresRevision);
            var pendingLast30 = last30.Count(a =>
                a.Status is ArticleSubmissionStatus.Submitted or ArticleSubmissionStatus.UnderReview);

            return Task.FromResult(new SubmissionTypeCounts(
                pending, receivedToday, receivedThisWeek, approvedLast30, rejectedLast30, pendingLast30));
        }
    }

    public Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsThisMonthAsync(
        DateTimeOffset monthStart,
        int maxCount,
        CancellationToken ct = default)
    {
        lock (sync)
        {
            IReadOnlyList<SubmissionContributor> result = submissions
                .Where(a => a.SubmittedAt.HasValue && a.SubmittedAt!.Value >= monthStart)
                .GroupBy(a => a.AuthorMemberId)
                .Select(g =>
                {
                    var member = resolveMember?.Invoke(g.Key);
                    return new SubmissionContributor(g.Key, member?.DisplayName ?? "Unknown member", g.Count());
                })
                .OrderByDescending(c => c.Count)
                .Take(maxCount)
                .ToList();

            return Task.FromResult(result);
        }
    }

    private static string Trim300(string value) =>
        value.Trim() is { Length: > 300 } s ? s[..300] : value.Trim();

    private static string? TrimOpt(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null
            : value.Trim() is { Length: > 0 } s && s.Length <= max ? s
            : value.Trim().Length > max ? value.Trim()[..max]
            : null;

    private static string GenerateSlug(string title) =>
        string.IsNullOrWhiteSpace(title) ? Guid.NewGuid().ToString("N")[..8] : NewsSlug.Slugify(title.Trim());

    private static ArticleSubmission Map(ArticleSubmissionEntity entity) =>
        new(
            entity.Id,
            entity.AuthorMemberId,
            entity.Title,
            entity.Slug,
            entity.Excerpt,
            entity.Body,
            entity.CoverImageBlobPath,
            entity.Tags,
            entity.Status,
            entity.SubmittedAt,
            entity.PublishedAt,
            entity.ReviewerEmail,
            entity.ReviewNotes,
            entity.RejectionReason,
            entity.Author?.DisplayName,
            entity.Author?.Email);
}
