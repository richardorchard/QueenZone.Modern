namespace QueenZone.NewsAgent;

public sealed class NewsAiDisabledException : InvalidOperationException
{
    public NewsAiDisabledException()
        : base("OpenRouter AI processing is disabled because no API key is configured.")
    {
    }
}

public sealed class NewsAiBudgetExceededException : InvalidOperationException
{
    public NewsAiBudgetExceededException(string message)
        : base(message)
    {
    }
}
