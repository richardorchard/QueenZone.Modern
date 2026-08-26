using QueenZone.Data;

namespace QueenZone.NewsAgent;

public interface INewsAgentGuidanceProvider
{
    Task<NewsAgentGuidanceSnapshot> GetPublishedAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken = default);

    void Invalidate(NewsAgentGuidanceType type);
}
