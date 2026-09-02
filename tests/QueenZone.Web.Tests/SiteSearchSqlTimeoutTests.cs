using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SiteSearchSqlTimeoutTests
{
    [Fact]
    public void IsCommandTimeout_detects_number_minus_two()
    {
        var timeout = CreateSqlException(
            SiteSearchSqlTimeout.SqlErrorNumber,
            "Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding.");

        Assert.True(SiteSearchSqlTimeout.IsCommandTimeout(timeout));
        Assert.True(SiteSearchSqlTimeout.IsCommandTimeout(new InvalidOperationException("wrapped", timeout)));
    }

    [Fact]
    public void IsCommandTimeout_detects_execution_timeout_message()
    {
        var timeout = CreateSqlException(0, "Execution Timeout Expired");

        Assert.True(SiteSearchSqlTimeout.IsCommandTimeout(timeout));
    }

    [Fact]
    public void IsCommandTimeout_ignores_other_sql_errors()
    {
        var missingObject = CreateSqlException(208, "Invalid object name.");

        Assert.False(SiteSearchSqlTimeout.IsCommandTimeout(missingObject));
        Assert.False(SiteSearchSqlTimeout.IsCommandTimeout(new InvalidOperationException("nope")));
        Assert.False(SiteSearchSqlTimeout.IsCommandTimeout(null));
    }

    [Fact]
    public void SanitizeQuery_strips_newlines_and_caps_length()
    {
        Assert.Equal(string.Empty, SiteSearchSqlTimeout.SanitizeQuery(null));
        Assert.Equal("Bohemian Rhapsody", SiteSearchSqlTimeout.SanitizeQuery("  Bohemian Rhapsody\r\n"));
        Assert.Equal(200, SiteSearchSqlTimeout.SanitizeQuery(new string('q', 250)).Length);
    }

    [Fact]
    public async Task ExecuteAsync_logs_warning_with_query_and_duration_then_throws()
    {
        var logger = new CollectingLogger<EfSiteSearchService>();
        var timeout = CreateSqlException(
            SiteSearchSqlTimeout.SqlErrorNumber,
            "Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding.");

        var thrown = await Assert.ThrowsAsync<SiteSearchTimeoutException>(() =>
            SiteSearchSqlTimeout.ExecuteAsync<SiteSearchPage>(
                _ => throw timeout,
                logger,
                "Bohemian Rhapsody\r\ninjection",
                CancellationToken.None));

        Assert.Equal("Bohemian Rhapsody injection", thrown.Query);
        Assert.True(thrown.Duration >= TimeSpan.Zero);
        Assert.Same(timeout, thrown.InnerException);

        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains("timed out after", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bohemian Rhapsody injection", warning.Message, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', warning.Message);
        Assert.Same(timeout, warning.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_returns_result_when_search_succeeds()
    {
        var logger = new CollectingLogger<EfSiteSearchService>();
        var page = new SiteSearchPage([], 0, 1, 20);

        var result = await SiteSearchSqlTimeout.ExecuteAsync(
            _ => Task.FromResult(page),
            logger,
            "queen",
            CancellationToken.None);

        Assert.Same(page, result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_does_not_swallow_non_timeout_failures()
    {
        var logger = new CollectingLogger<EfSiteSearchService>();
        var sql = CreateSqlException(208, "Invalid object name.");

        var thrown = await Assert.ThrowsAsync<SqlException>(() =>
            SiteSearchSqlTimeout.ExecuteAsync<SiteSearchPage>(
                _ => throw sql,
                logger,
                "queen",
                CancellationToken.None));

        Assert.Same(sql, thrown);
        Assert.Empty(logger.Entries);
    }

    internal static SqlException CreateSqlException(int number, string message)
    {
        var sqlClient = typeof(SqlException).Assembly;
        var errorCollectionType = sqlClient.GetType("Microsoft.Data.SqlClient.SqlErrorCollection")
            ?? throw new InvalidOperationException("SqlErrorCollection type not found.");
        var errorType = sqlClient.GetType("Microsoft.Data.SqlClient.SqlError")
            ?? throw new InvalidOperationException("SqlError type not found.");

        var collection = Activator.CreateInstance(errorCollectionType, nonPublic: true)
            ?? throw new InvalidOperationException("Unable to create SqlErrorCollection.");

        var errorCtor = errorType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var errorArgs = errorCtor.GetParameters().Select(p =>
        {
            if (p.Name is "infoNumber" or "number")
            {
                return number;
            }

            if (p.ParameterType == typeof(int))
            {
                return 0;
            }

            if (p.ParameterType == typeof(byte))
            {
                return (byte)16;
            }

            if (p.ParameterType == typeof(string))
            {
                return p.Name is "errorMessage" or "message" ? message : "server";
            }

            if (p.ParameterType == typeof(uint))
            {
                return 0u;
            }

            if (typeof(Exception).IsAssignableFrom(p.ParameterType))
            {
                return null!;
            }

            return p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)! : null!;
        }).ToArray();
        var error = errorCtor.Invoke(errorArgs);

        errorCollectionType
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(collection, [error]);

        var createException = typeof(SqlException)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "CreateException")
            .OrderBy(m => m.GetParameters().Length)
            .First();
        var createArgs = createException.GetParameters().Select(p =>
        {
            if (p.ParameterType == errorCollectionType)
            {
                return collection;
            }

            if (p.ParameterType == typeof(string))
            {
                return "12.0.0";
            }

            if (p.ParameterType == typeof(Guid))
            {
                return Guid.Empty;
            }

            return null!;
        }).ToArray();

        return (SqlException)createException.Invoke(null, createArgs)!;
    }
}
