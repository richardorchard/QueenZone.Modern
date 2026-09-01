using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class RequestLogScopeMiddlewareTests
{
    [Fact]
    public async Task Member_principal_scope_has_trace_id_and_member_id_without_pii()
    {
        var memberId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var logger = new RecordingScopeLogger();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "member-trace",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, memberId.ToString("D")),
                    new Claim(ClaimTypes.Email, "member@example.com"),
                    new Claim(ClaimTypes.Name, "Display Name"),
                ],
                authenticationType: "MembersCookie")),
        };
        context.Request.Path = "/news";
        var invoked = false;
        var middleware = new RequestLogScopeMiddleware(
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(Activity.Current?.TraceId.ToString() ?? "member-trace", scope["TraceId"]);
        Assert.Equal(memberId.ToString("D"), scope["MemberId"]);
        Assert.False(scope.ContainsKey("Email"));
        Assert.False(scope.ContainsKey("Name"));
        Assert.False(scope.ContainsKey("DisplayName"));
        Assert.DoesNotContain(scope.Values, value => value is string text
            && (text.Contains("member@example.com", StringComparison.Ordinal)
                || text.Contains("Display Name", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Anonymous_request_scope_has_trace_id_only()
    {
        var logger = new RecordingScopeLogger();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "anon-trace",
        };
        context.Request.Path = "/";
        var middleware = new RequestLogScopeMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(Activity.Current?.TraceId.ToString() ?? "anon-trace", scope["TraceId"]);
        Assert.False(scope.ContainsKey("MemberId"));
        Assert.Equal(["TraceId"], scope.Keys);
    }

    [Fact]
    public async Task Entra_admin_without_member_guid_has_trace_id_only()
    {
        var logger = new RecordingScopeLogger();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "admin-trace",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Email, "admin@example.com"),
                    new Claim(ClaimTypes.Name, "Admin User"),
                    new Claim(ClaimTypes.NameIdentifier, "admin@example.com"),
                ],
                authenticationType: "OpenIdConnect")),
        };
        context.Request.Path = "/admin/news";
        var middleware = new RequestLogScopeMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(Activity.Current?.TraceId.ToString() ?? "admin-trace", scope["TraceId"]);
        Assert.False(scope.ContainsKey("MemberId"));
        Assert.DoesNotContain(scope.Keys, key => key.Contains("Email", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Name", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(scope.Values, value => value is string text
            && text.Contains("admin@example.com", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/warmup")]
    public async Task Probe_paths_do_not_open_a_log_scope(string path)
    {
        var logger = new RecordingScopeLogger();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "probe-trace",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D"))],
                authenticationType: "MembersCookie")),
        };
        context.Request.Path = path;
        var invoked = false;
        var middleware = new RequestLogScopeMiddleware(
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        Assert.Empty(logger.Scopes);
    }

    private sealed class RecordingScopeLogger : ILogger<RequestLogScopeMiddleware>
    {
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                Scopes.Add(pairs.ToDictionary(static pair => pair.Key, static pair => pair.Value));
            }

            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
