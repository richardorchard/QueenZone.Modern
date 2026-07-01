namespace QueenZone.NewsAgent;

public sealed record NewsAiChatMessage(string Role, string Content);

public sealed record NewsAiChatRequest(
    NewsAiModelRole ModelRole,
    string PromptVersion,
    IReadOnlyList<NewsAiChatMessage> Messages,
    int? MaxOutputTokens = null);

public sealed record NewsAiChatCompletion(
    string Content,
    string ModelId,
    int InputTokens,
    int OutputTokens,
    decimal? EstimatedCostUsd,
    bool DryRun);

public sealed record NewsAiRunExecutionResult(
    int AiRunId,
    NewsAiChatCompletion Completion);
