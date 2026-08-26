using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

internal sealed class FakeNewsAgentGuidanceProvider(NewsAgentGuidanceSnapshot snapshot) : INewsAgentGuidanceProvider
{
    public Task<NewsAgentGuidanceSnapshot> GetPublishedAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(snapshot);

    public void Invalidate(NewsAgentGuidanceType type)
    {
    }
}
