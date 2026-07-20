using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfArticleRepository(QueenZoneDbContext dbContext) : IArticleRepository
{
    public async Task<int> GetCountAsync(string? tag = null, CancellationToken ct = default)
    {
        var query = Published();
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var rough = await query
                .Where(a => a.Tags != null && a.Tags.Contains(tag))
                .Select(a => new { a.Tags })
                .ToListAsync(ct);
            return rough.Count(a => HasTag(a.Tags, tag));
        }

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<PublishedArticleSubmission>> GetPageAsync(
        int page, int pageSize, string? tag = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var roughRows = await Published()
                .OrderByDescending(a => a.PublishedAt)
                .Where(a => a.Tags != null && a.Tags.Contains(tag))
                .Select(a => new
                {
                    a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath,
                    a.Tags, a.PublishedAt,
                    DisplayName = a.Author != null ? a.Author.DisplayName : string.Empty,
                })
                .ToListAsync(ct);

            return roughRows
                .Where(a => HasTag(a.Tags, tag))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => Map(a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath, a.Tags, a.PublishedAt!.Value, a.DisplayName))
                .ToList();
        }

        var rows = await Published()
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath,
                a.Tags, a.PublishedAt,
                DisplayName = a.Author != null ? a.Author.DisplayName : string.Empty,
            })
            .ToListAsync(ct);

        return rows
            .Select(a => Map(a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath, a.Tags, a.PublishedAt!.Value, a.DisplayName))
            .ToList();
    }

    public async Task<PublishedArticleSubmission?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var a = await Published()
            .Where(x => x.Slug == slug)
            .Select(x => new
            {
                x.Id, x.Title, x.Slug, x.Excerpt, x.Body, x.CoverImageBlobPath,
                x.Tags, x.PublishedAt,
                DisplayName = x.Author != null ? x.Author.DisplayName : string.Empty,
            })
            .FirstOrDefaultAsync(ct);

        return a is null
            ? null
            : Map(a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath, a.Tags, a.PublishedAt!.Value, a.DisplayName);
    }

    public async Task<(PublishedArticleSubmission? Previous, PublishedArticleSubmission? Next)> GetAdjacentAsync(
        DateTimeOffset publishedAt, CancellationToken ct = default)
    {
        var prevTask = Published()
            .Where(a => a.PublishedAt < publishedAt)
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new
            {
                a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath,
                a.Tags, a.PublishedAt,
                DisplayName = a.Author != null ? a.Author.DisplayName : string.Empty,
            })
            .FirstOrDefaultAsync(ct);

        var nextTask = Published()
            .Where(a => a.PublishedAt > publishedAt)
            .OrderBy(a => a.PublishedAt)
            .Select(a => new
            {
                a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath,
                a.Tags, a.PublishedAt,
                DisplayName = a.Author != null ? a.Author.DisplayName : string.Empty,
            })
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(prevTask, nextTask);

        var prev = prevTask.Result;
        var next = nextTask.Result;

        return (
            prev is null ? null : Map(prev.Id, prev.Title, prev.Slug, prev.Excerpt, prev.Body, prev.CoverImageBlobPath, prev.Tags, prev.PublishedAt!.Value, prev.DisplayName),
            next is null ? null : Map(next.Id, next.Title, next.Slug, next.Excerpt, next.Body, next.CoverImageBlobPath, next.Tags, next.PublishedAt!.Value, next.DisplayName)
        );
    }

    public async Task<IReadOnlyList<PublishedArticleSubmission>> GetSitemapEntriesAsync(CancellationToken ct = default)
    {
        var rows = await Published()
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new
            {
                a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath,
                a.Tags, a.PublishedAt,
                DisplayName = a.Author != null ? a.Author.DisplayName : string.Empty,
            })
            .ToListAsync(ct);

        return rows
            .Select(a => Map(a.Id, a.Title, a.Slug, a.Excerpt, a.Body, a.CoverImageBlobPath, a.Tags, a.PublishedAt!.Value, a.DisplayName))
            .ToList();
    }

    private IQueryable<ArticleSubmissionEntity> Published() =>
        dbContext.ArticleSubmissions
            .AsNoTracking()
            .Include(a => a.Author)
            .Where(a => a.Status == ArticleSubmissionStatus.Published && a.PublishedAt != null);

    private static bool HasTag(string? tags, string tag) =>
        !string.IsNullOrWhiteSpace(tags) &&
        ("," + tags + ",").Contains("," + tag + ",", StringComparison.OrdinalIgnoreCase);

    private static PublishedArticleSubmission Map(
        Guid id, string title, string slug, string? excerpt, string body,
        string? coverImageBlobPath, string? tags, DateTimeOffset publishedAt, string displayName) =>
        new(
            id, title, slug, excerpt, body, coverImageBlobPath, tags, publishedAt,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            EfArticleSubmissionRepository.EstimateWordCount(body));
}
