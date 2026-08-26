using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

internal sealed class EmptyNewsAgentGuidanceProvider : INewsAgentGuidanceProvider
{
    public static EmptyNewsAgentGuidanceProvider Instance { get; } = new();

    public Task<NewsAgentGuidanceSnapshot> GetPublishedAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(NewsAgentGuidanceSnapshot.Empty);

    public void Invalidate(NewsAgentGuidanceType type)
    {
    }
}
