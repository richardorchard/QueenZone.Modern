using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace QueenZone.Data;

/// <summary>
/// Detects <c>SqlException</c> command timeouts from <c>dbo.SearchDocument_Search</c>
/// and logs the query (sanitized) plus duration at Warning.
/// </summary>
public static class SiteSearchSqlTimeout
{
    /// <summary>Client command-timeout number for <c>Execution Timeout Expired</c>.</summary>
    public const int SqlErrorNumber = -2;

    public static bool IsCommandTimeout(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql
                && (sql.Number == SqlErrorNumber || IsTimeoutMessage(sql.Message)))
            {
                return true;
            }
        }

        return false;
    }

    public static string SanitizeQuery(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return string.Empty;
        }

        var sanitized = query
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Trim();

        const int maxLength = 200;
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }

    public static SiteSearchTimeoutException CreateAndLog(
        ILogger logger,
        string query,
        TimeSpan duration,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(exception);

        var safeQuery = SanitizeQuery(query);
        logger.LogWarning(
            exception,
            "Site search SQL command timed out after {DurationMs}ms for query {Query}",
            (int)Math.Round(duration.TotalMilliseconds),
            safeQuery);
        return new SiteSearchTimeoutException(safeQuery, duration, exception);
    }

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> execute,
        ILogger logger,
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(logger);

        var started = Stopwatch.StartNew();
        try
        {
            return await execute(cancellationToken);
        }
        catch (Exception ex) when (IsCommandTimeout(ex))
        {
            throw CreateAndLog(logger, query, started.Elapsed, ex);
        }
    }

    private static bool IsTimeoutMessage(string? message) =>
        !string.IsNullOrEmpty(message)
        && message.Contains("Execution Timeout Expired", StringComparison.OrdinalIgnoreCase);
}
