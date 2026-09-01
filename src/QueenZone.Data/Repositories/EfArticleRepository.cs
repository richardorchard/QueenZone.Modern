using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfArticleRepository(QueenZoneDbContext dbContext, IEditorialArticleRepository? editorialArticles = null) : IArticleRepository
{
    public async Task<int> GetCountAsync(string? tag = null, CancellationToken ct = default)
    {
        var all = await GetAllPublishedAsync(ct);
        return string.IsNullOrWhiteSpace(tag) ? all.Count : all.Count(a => HasTag(a.Tags, tag));
    }

    public async Task<IReadOnlyList<PublishedArticleSubmission>> GetPageAsync(
        int page, int pageSize, string? tag = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await GetAllPublishedAsync(ct);

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
        var editorial = editorialArticles is null ? null : await editorialArticles.GetPublishedBySlugAsync(slug, ct);
        if (editorial is not null) return MapEditorial(editorial);
        var rows = await SelectProjection(Published().Where(x => x.Slug == slug)).ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public async Task<(PublishedArticleSubmission? Previous, PublishedArticleSubmission? Next)> GetAdjacentAsync(
        DateTimeOffset publishedAt, CancellationToken ct = default)
    {
        var all = await GetAllPublishedAsync(ct);

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
        var rows = await GetAllPublishedAsync(ct);
        return rows.OrderByDescending(a => a.PublishedAt).ToList();
    }

    private IQueryable<ArticleSubmissionEntity> Published() =>
        dbContext.ArticleSubmissions
            .AsNoTracking()
            .Where(a => a.Status == ArticleSubmissionStatus.Published && a.PublishedAt != null
                && !dbContext.EditorialArticles.Any(e => e.SourceSubmissionId == a.Id
                    && e.LiveTitle != null && e.Status != EditorialArticleStatus.Unpublished));

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

    private static PublishedArticleSubmission MapEditorial(EditorialArticle x) => new(
        x.Id, x.Title, x.Slug, x.Excerpt, x.Body, x.ImageBlobKey, x.Tags,
        x.PublishedAt, x.AuthorName, EfArticleSubmissionRepository.EstimateWordCount(x.Body), Category: x.Category, Source: x.Source);

    private async Task<List<PublishedArticleSubmission>> GetAllPublishedAsync(CancellationToken ct)
    {
        var rows = await SelectProjection(Published()).ToListAsync(ct);
        if (editorialArticles is not null)
        {
            rows.AddRange((await editorialArticles.GetPublishedStandaloneAsync(ct)).Select(MapEditorial));
        }
        return rows;
    }
}
