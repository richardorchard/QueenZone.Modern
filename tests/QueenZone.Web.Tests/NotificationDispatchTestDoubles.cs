using Microsoft.Extensions.Logging;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

internal sealed class NoOpNotificationDispatcher : INotificationDispatcher
{
    public static NoOpNotificationDispatcher Instance { get; } = new();

    public Task NotifyForumReplyAsync(
        int topicId,
        int postId,
        Guid authorMemberId,
        string topicTitle,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyPrivateMessageAsync(
        Guid conversationId,
        Guid recipientMemberId,
        Guid senderMemberId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyNewsPublishedAsync(
        int articleId,
        string title,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class RecordingPushTransport : IPushTransport
{
    public List<(IReadOnlyList<PushDeviceToken> Tokens, PushNotificationPayload Payload)> Sends { get; } = [];

    public Exception? ThrowOnSend { get; set; }

    public int TokenSendCount => Sends.Sum(send => send.Tokens.Count);

    public Task SendAsync(
        IReadOnlyList<PushDeviceToken> tokens,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend is not null)
        {
            throw ThrowOnSend;
        }

        Sends.Add(([.. tokens], payload));
        return Task.CompletedTask;
    }
}

internal sealed class FakeTopicWatchLookup : ITopicWatchLookup
{
    public Dictionary<int, IReadOnlyList<Guid>> Watchers { get; } = [];

    public Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Watchers.TryGetValue(topicId, out var members)
                ? members
                : []);
}

internal sealed class CollectingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

internal sealed class AlwaysWatchLookup(Guid memberId) : ITopicWatchLookup
{
    public Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([memberId]);
}

internal static class DeviceTokenTestData
{
    public static DeviceTokenEntity Token(
        Guid memberId,
        QueenZone.Data.Entities.DevicePushPlatform platform,
        string token,
        string? deviceId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId ?? $"device-{Guid.NewGuid():N}",
            MemberAccountId = memberId,
            Platform = platform,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public static PushDeviceToken PushToken(
        Guid memberId,
        PushDevicePlatform platform,
        string token) =>
        new(memberId, platform, token);
}
