namespace QueenZone.NewsAgent;

public interface INewsDiscoveryHttpClient
{
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default);
}

public sealed class NewsDiscoveryHttpClient(HttpClient httpClient) : INewsDiscoveryHttpClient
{
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
