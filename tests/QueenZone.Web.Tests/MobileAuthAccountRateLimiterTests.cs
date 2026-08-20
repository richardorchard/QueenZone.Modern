using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QueenZone.Web.Tests;

public sealed class MobileAuthAccountRateLimiterTests
{
    [Fact]
    public void IsAllowed_limits_per_member_and_logs_without_secrets()
    {
        var logger = new RecordingLogger();
        var limiter = new MobileAuthAccountRateLimiter(
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            Options.Create(new AuthRateLimitingOptions { AccountPermitLimit = 1, AccountWindowMinutes = 60 }),
            logger);
        var member = Guid.NewGuid();
        var other = Guid.NewGuid();

        Assert.True(limiter.IsAllowed(member));
        Assert.False(limiter.IsAllowed(member));
        Assert.True(limiter.IsAllowed(other));
        Assert.Contains(logger.Messages, message => message.Contains(member.ToString("N"), StringComparison.OrdinalIgnoreCase)
            || message.Contains(member.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.All(logger.Messages, message =>
        {
            Assert.DoesNotContain("refresh", message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Bearer", message, StringComparison.Ordinal);
        });
    }

    private sealed class RecordingLogger : ILogger<MobileAuthAccountRateLimiter>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
