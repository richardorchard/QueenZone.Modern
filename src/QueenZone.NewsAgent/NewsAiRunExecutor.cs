using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsAiRunExecutor(
    INewsAiClient aiClient,
    INewsDiscoveryRepository repository,
    NewsAiBudgetGuard budgetGuard,
    IOptions<OpenRouterOptions> options,
    ILogger<NewsAiRunExecutor> logger)
{
    public const string ModelProvider = "openrouter";

    public bool IsAiEnabled => aiClient.IsEnabled;

    public void BeginRun() => budgetGuard.BeginRun();

    public async Task<NewsAiRunExecutionResult> ExecuteAsync(
        int candidateId,
        NewsAiRunKind kind,
        NewsAiModelRole modelRole,
        string promptVersion,
        IReadOnlyList<NewsAiChatMessage> messages,
        DateTime? runAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (!aiClient.IsEnabled)
        {
            throw new NewsAiDisabledException();
        }

        var utcNow = runAtUtc ?? DateTime.UtcNow;
        budgetGuard.RegisterCandidateAttempt();
        await budgetGuard.EnsureWithinBudgetAsync(utcNow, cancellationToken);

        var modelId = options.Value.ResolveModel(modelRole);
        var aiRunId = await repository.CreateAiRunAsync(
            new NewsAiRunCreateRequest(
                candidateId,
                kind,
                ModelProvider,
                modelId,
                promptVersion,
                utcNow),
            cancellationToken);

        try
        {
            var completion = await aiClient.CompleteChatAsync(
                new NewsAiChatRequest(modelRole, promptVersion, messages),
                cancellationToken);

            budgetGuard.RegisterSpend(completion.EstimatedCostUsd);

            await repository.CompleteAiRunAsync(
                aiRunId,
                new NewsAiRunCompletion(
                    NewsAiRunStatus.Succeeded,
                    completion.InputTokens,
                    completion.OutputTokens,
                    completion.EstimatedCostUsd,
                    completion.DryRun ? null : completion.Content,
                    null,
                    DateTime.UtcNow),
                cancellationToken);

            logger.LogInformation(
                "AI run {AiRunId} completed for candidate {CandidateId} using model {ModelId}.",
                aiRunId,
                candidateId,
                completion.ModelId);

            return new NewsAiRunExecutionResult(aiRunId, completion);
        }
        catch (Exception ex)
        {
            await repository.CompleteAiRunAsync(
                aiRunId,
                new NewsAiRunCompletion(
                    NewsAiRunStatus.Failed,
                    null,
                    null,
                    null,
                    null,
                    ex.Message,
                    DateTime.UtcNow),
                cancellationToken);

            logger.LogError(
                ex,
                "AI run {AiRunId} failed for candidate {CandidateId} using model {ModelId}.",
                aiRunId,
                candidateId,
                modelId);

            throw;
        }
    }
}
