namespace QueenZone.NewsAgent;

public interface INewsAiClient
{
    bool IsEnabled { get; }

    Task<NewsAiChatCompletion> CompleteChatAsync(
        NewsAiChatRequest request,
        CancellationToken cancellationToken = default);
}
