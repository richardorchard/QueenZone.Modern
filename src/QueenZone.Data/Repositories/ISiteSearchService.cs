namespace QueenZone.Data;

/// <summary>Globally ranked whole-site search against the shared <c>SearchDocument</c> index.</summary>
public interface ISiteSearchService
{
    Task<SiteSearchPage> SearchAsync(
        string query,
        string? contentType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
