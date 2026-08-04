using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data;

/// <summary>Calls <c>dbo.SearchDocument_Search</c> for ranked, paginated whole-site search.</summary>
public sealed class EfSiteSearchService(QueenZoneDbContext dbContext) : ISiteSearchService
{
    private const int MaxPageSize = 100;

    public async Task<SiteSearchPage> SearchAsync(
        string query,
        string? contentType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SiteSearchPage([], 0, page, pageSize);
        }

        var normalizedPage = Math.Max(page, 1);
        var take = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (normalizedPage - 1) * take;

        return await ExecuteSearchAsync(query.Trim(), contentType, offset, take, normalizedPage, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private async Task<SiteSearchPage> ExecuteSearchAsync(
        string query,
        string? contentType,
        int offset,
        int take,
        int normalizedPage,
        CancellationToken cancellationToken)
    {
        var totalRecords = EfSql.OutputInt("@TotalRecords");

        var rows = await EfSql.QueryProcAsync<SiteSearchRow>(
            dbContext,
            "SearchDocument_Search",
            command =>
            {
                command.Parameters.Add(EfSql.Input("@Query", query));
                command.Parameters.Add(EfSql.Input("@ContentType", contentType));
                command.Parameters.Add(EfSql.Input("@Offset", offset));
                command.Parameters.Add(EfSql.Input("@PageSize", take));
                command.Parameters.Add(totalRecords);
            },
            cancellationToken: cancellationToken);

        var results = rows.Select(Map).ToList();
        return new SiteSearchPage(
            results,
            EfSql.GetNullableInt(totalRecords) ?? 0,
            normalizedPage,
            take);
    }

    private static SiteSearchResult Map(SiteSearchRow row) =>
        new(
            row.ContentType,
            row.Title,
            row.Summary ?? string.Empty,
            row.Url,
            row.PublishedAt,
            row.ImageUrl,
            row.Category,
            row.AuthorDisplayName);

    internal sealed class SiteSearchRow
    {
        public string ContentType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Summary { get; set; }

        public string Url { get; set; } = string.Empty;

        public DateTimeOffset? PublishedAt { get; set; }

        public string? ImageUrl { get; set; }

        public string? Category { get; set; }

        public string? AuthorDisplayName { get; set; }
    }
}
