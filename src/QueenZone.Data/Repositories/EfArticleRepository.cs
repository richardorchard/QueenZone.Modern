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

        var rows = await SelectProjection(Published()).ToListAsync(ct);

        var filtered = string.IsNullOrWhiteSpace(tag)
            ? rows
            : rows.Where(a => HasTag(a.Tags, tag)).ToList();

        return filtered
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<PublishedArticleSubmission?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var rows = await SelectProjection(Published().Where(x => x.Slug == slug)).ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public async Task<(PublishedArticleSubmission? Previous, PublishedArticleSubmission? Next)> GetAdjacentAsync(
        DateTimeOffset publishedAt, CancellationToken ct = default)
    {
        var all = await SelectProjection(Published()).ToListAsync(ct);

        var prev = all
            .Where(a => a.PublishedAt < publishedAt)
            .OrderByDescending(a => a.PublishedAt)
            .FirstOrDefault();
        var next = all
            .Where(a => a.PublishedAt > publishedAt)
            .OrderBy(a => a.PublishedAt)
            .FirstOrDefault();

        return (prev, next);
    }

    public async Task<IReadOnlyList<PublishedArticleSubmission>> GetSitemapEntriesAsync(CancellationToken ct = default)
    {
        var rows = await SelectProjection(Published()).ToListAsync(ct);
        return rows.OrderByDescending(a => a.PublishedAt).ToList();
    }

    private IQueryable<ArticleSubmissionEntity> Published() =>
        dbContext.ArticleSubmissions
            .AsNoTracking()
            .Where(a => a.Status == ArticleSubmissionStatus.Published && a.PublishedAt != null);

    // Anonymous-type projection lets EF Core generate a simple JOIN without implicit ORDER BY.
    // The OrderBy is applied client-side after materialisation to avoid SQLite's DateTimeOffset
    // ORDER BY limitation.
    private static IQueryable<PublishedArticleSubmission> SelectProjection(
        IQueryable<ArticleSubmissionEntity> query) =>
        query.Select(a => new PublishedArticleSubmission(
            a.Id,
            a.Title,
            a.Slug,
            a.Excerpt,
            a.Body,
            a.CoverImageBlobPath,
            a.Tags,
            a.PublishedAt!.Value,
            string.IsNullOrWhiteSpace(a.Author != null ? a.Author.DisplayName : null) ? null : a.Author!.DisplayName,
            EfArticleSubmissionRepository.EstimateWordCount(a.Body)));

    private static bool HasTag(string? tags, string tag) =>
        !string.IsNullOrWhiteSpace(tags) &&
        ("," + tags + ",").Contains("," + tag + ",", StringComparison.OrdinalIgnoreCase);
}
