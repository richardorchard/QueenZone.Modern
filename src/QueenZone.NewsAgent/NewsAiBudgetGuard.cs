using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsAiBudgetGuard(
    INewsDiscoveryRepository repository,
    IOptions<OpenRouterOptions> options)
{
    private int candidatesProcessed;
    private decimal runSpendUsd;

    public void BeginRun()
    {
        candidatesProcessed = 0;
        runSpendUsd = 0m;
    }

    public void RegisterCandidateAttempt()
    {
        candidatesProcessed++;
        if (candidatesProcessed > options.Value.PerRunCandidateLimit)
        {
            throw new NewsAiBudgetExceededException(
                $"Per-run candidate limit of {options.Value.PerRunCandidateLimit} exceeded.");
        }
    }

    public async Task EnsureWithinBudgetAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (runSpendUsd >= settings.PerRunBudgetUsd)
        {
            throw new NewsAiBudgetExceededException(
                $"Per-run budget of ${settings.PerRunBudgetUsd:0.00} exceeded.");
        }

        var dayStart = utcNow.Date;
        var dayEnd = dayStart.AddDays(1);
        var dailySpend = await repository.GetEstimatedAiSpendUsdAsync(dayStart, dayEnd, cancellationToken);
        if (dailySpend + runSpendUsd >= settings.DailyBudgetUsd)
        {
            throw new NewsAiBudgetExceededException(
                $"Daily AI budget of ${settings.DailyBudgetUsd:0.00} exceeded.");
        }
    }

    public void RegisterSpend(decimal? estimatedCostUsd)
    {
        if (estimatedCostUsd is null or <= 0m)
        {
            return;
        }

        runSpendUsd += estimatedCostUsd.Value;
        if (runSpendUsd > options.Value.PerRunBudgetUsd)
        {
            throw new NewsAiBudgetExceededException(
                $"Per-run budget of ${options.Value.PerRunBudgetUsd:0.00} exceeded.");
        }
    }

    public int CandidatesProcessed => candidatesProcessed;

    public decimal RunSpendUsd => runSpendUsd;
}
