using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Resolves modern contributor credit from approved <see cref="FanPerformanceSubmission"/>
/// rows. Legacy archive performances stay uncredited when no matching submission exists.
/// </summary>
public sealed class FanPerformanceCreditResolver(IFanPerformanceSubmissionRepository submissions)
{
    public async Task<IReadOnlyList<FanPerformance>> EnrichAsync(
        IReadOnlyList<FanPerformance> performances,
        CancellationToken cancellationToken = default)
    {
        if (performances.Count == 0)
        {
            return performances;
        }

        var credits = await submissions.GetApprovedContributorCreditsAsync(
            performances.Select(performance => performance.Id).ToArray(),
            cancellationToken);
        return FanPerformanceCredits.Apply(performances, credits);
    }

    public async Task<FanPerformance?> EnrichOneAsync(
        FanPerformance? performance,
        CancellationToken cancellationToken = default)
    {
        if (performance is null)
        {
            return null;
        }

        var enriched = await EnrichAsync([performance], cancellationToken);
        return enriched[0];
    }
}
