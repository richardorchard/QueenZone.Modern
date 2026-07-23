using System.Text;

namespace QueenZone.NewsAgent;

public interface INewsDiscoveryHttpClient
{
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default);
}

public sealed class NewsDiscoveryHttpClient(HttpClient httpClient) : INewsDiscoveryHttpClient
{
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        OutboundUrlSafety.EnsureAllowedHttpUrl(url);

        using var response = await httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        // Cap body size to limit memory/DoS from a malicious feed/page.
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken)) > 0)
        {
            total += read;
            if (total > OutboundUrlSafety.DefaultMaxResponseBytes)
            {
                throw new InvalidOperationException(
                    $"Discovery response from '{url}' exceeds the {OutboundUrlSafety.DefaultMaxResponseBytes}-byte limit.");
            }

            buffer.Write(chunk, 0, read);
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
}
