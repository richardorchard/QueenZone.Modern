using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class OpenRouterNewsAiClientTests
{
    [Fact]
    public async Task CompleteChatAsync_throws_when_api_key_missing()
    {
        var client = CreateClient(
            (_, _) => throw new InvalidOperationException("HTTP should not be called."),
            new OpenRouterOptions());

        await Assert.ThrowsAsync<NewsAiDisabledException>(() => client.CompleteChatAsync(CreateRequest()));
    }

    [Fact]
    public async Task CompleteChatAsync_dry_run_skips_provider_call()
    {
        var client = CreateClient(
            (_, _) => throw new InvalidOperationException("HTTP should not be called."),
            new OpenRouterOptions
            {
                ApiKey = "test-key",
                DryRun = true
            });

        var completion = await client.CompleteChatAsync(CreateRequest());

        Assert.True(completion.DryRun);
        Assert.Equal("openai/gpt-4.1-nano", completion.ModelId);
        Assert.Equal(0, completion.InputTokens);
    }

    [Fact]
    public async Task CompleteChatAsync_parses_successful_provider_response()
    {
        const string responseBody = """
            {
              "model": "openai/gpt-4.1-nano",
              "choices": [
                {
                  "message": {
                    "content": "{\"relevant\":true}"
                  }
                }
              ],
              "usage": {
                "prompt_tokens": 120,
                "completion_tokens": 30,
                "cost": 0.0004
              }
            }
            """;

        var client = CreateClient(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            },
            new OpenRouterOptions
            {
                ApiKey = "test-key",
                MaxRetryAttempts = 1
            });

        var completion = await client.CompleteChatAsync(CreateRequest());

        Assert.False(completion.DryRun);
        Assert.Equal("{\"relevant\":true}", completion.Content);
        Assert.Equal(120, completion.InputTokens);
        Assert.Equal(30, completion.OutputTokens);
        Assert.Equal(0.0004m, completion.EstimatedCostUsd);
    }

    [Fact]
    public async Task CompleteChatAsync_retries_transient_failures()
    {
        var attempts = 0;
        const string responseBody = """
            {
              "model": "openai/gpt-4.1-nano",
              "choices": [{ "message": { "content": "ok" } }],
              "usage": { "prompt_tokens": 1, "completion_tokens": 1 }
            }
            """;

        var client = CreateClient(
            (_, _) =>
            {
                attempts++;
                if (attempts == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
            },
            new OpenRouterOptions
            {
                ApiKey = "test-key",
                MaxRetryAttempts = 2
            });

        var completion = await client.CompleteChatAsync(CreateRequest());

        Assert.Equal("ok", completion.Content);
        Assert.Equal(2, attempts);
    }

    private static OpenRouterNewsAiClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler,
        OpenRouterOptions options)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(handler));
        return new OpenRouterNewsAiClient(
            httpClient,
            Options.Create(options),
            NullLogger<OpenRouterNewsAiClient>.Instance);
    }

    private static NewsAiChatRequest CreateRequest() =>
        new(
            NewsAiModelRole.Triage,
            "triage-v1",
            [new NewsAiChatMessage("user", "Classify this headline.")]);

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
