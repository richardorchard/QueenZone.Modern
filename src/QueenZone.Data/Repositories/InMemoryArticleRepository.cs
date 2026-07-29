namespace QueenZone.Data;

public sealed class InMemoryArticleRepository(IArticleSubmissionRepository submissionRepository) : IArticleRepository
{
    public async Task<int> GetCountAsync(string? tag = null, CancellationToken ct = default)
    {
        var all = await submissionRepository.GetPublishedAsync(ct);
        return string.IsNullOrWhiteSpace(tag)
            ? all.Count
            : all.Count(a => HasTag(a.Tags, tag));
    }

    public async Task<IReadOnlyList<PublishedArticleSubmission>> GetPageAsync(
        int page, int pageSize, string? tag = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var all = await submissionRepository.GetPublishedAsync(ct);
        IEnumerable<PublishedArticleSubmission> filtered = string.IsNullOrWhiteSpace(tag)
            ? all
            : all.Where(a => HasTag(a.Tags, tag));

        return filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<PublishedArticleSubmission?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var all = await submissionRepository.GetPublishedAsync(ct);
        return all.FirstOrDefault(a => string.Equals(a.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(PublishedArticleSubmission? Previous, PublishedArticleSubmission? Next)> GetAdjacentAsync(
        DateTimeOffset publishedAt, CancellationToken ct = default)
    {
        var all = await submissionRepository.GetPublishedAsync(ct);
        // all is ordered descending by PublishedAt
        var prev = all.FirstOrDefault(a => a.PublishedAt < publishedAt);
        var next = all.LastOrDefault(a => a.PublishedAt > publishedAt);
        return (prev, next);
    }

    public async Task<IReadOnlyList<PublishedArticleSubmission>> GetSitemapEntriesAsync(CancellationToken ct = default) =>
        await submissionRepository.GetPublishedAsync(ct);

    private static bool HasTag(string? tags, string tag) =>
        !string.IsNullOrWhiteSpace(tags) &&
        ("," + tags + ",").Contains("," + tag + ",", StringComparison.OrdinalIgnoreCase);
}
