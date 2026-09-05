namespace QueenZone.Data;

public sealed record FanPerformanceContributorCredit(Guid MemberId, string DisplayName);

public static class FanPerformanceCredits
{
    public static IReadOnlyList<FanPerformance> Apply(
        IReadOnlyList<FanPerformance> performances,
        IReadOnlyDictionary<int, FanPerformanceContributorCredit> credits)
    {
        ArgumentNullException.ThrowIfNull(performances);
        ArgumentNullException.ThrowIfNull(credits);

        if (performances.Count == 0 || credits.Count == 0)
        {
            return performances;
        }

        return performances.Select(performance => Apply(performance, credits)).ToList();
    }

    public static FanPerformance Apply(
        FanPerformance performance,
        IReadOnlyDictionary<int, FanPerformanceContributorCredit> credits)
    {
        ArgumentNullException.ThrowIfNull(performance);
        ArgumentNullException.ThrowIfNull(credits);

        return credits.TryGetValue(performance.Id, out var credit)
            ? performance with
            {
                ContributorMemberId = credit.MemberId,
                ContributorDisplayName = credit.DisplayName,
            }
            : performance;
    }
}
