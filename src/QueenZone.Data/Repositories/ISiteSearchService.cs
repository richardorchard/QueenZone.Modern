namespace QueenZone.Data;

/// <summary>Globally ranked whole-site search against the shared <c>SearchDocument</c> index.</summary>
/// <remarks>
/// SQL command timeouts (<see cref="Microsoft.Data.SqlClient.SqlException"/> number -2 /
/// Execution Timeout Expired) are surfaced as <see cref="SiteSearchTimeoutException"/>.
/// </remarks>
public interface ISiteSearchService
{
    Task<SiteSearchPage> SearchAsync(
        string query,
        string? contentType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
