using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Google.Analytics.Data.V1Beta;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

[ExcludeFromCodeCoverage(Justification = "Thin Google SDK adapter; dashboard behavior and caching are covered with an injectable client.")]
internal sealed class GoogleAnalyticsDataClient(IOptions<AnalyticsOptions> options) : IGoogleAnalyticsDataClient
{
    public async Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(
        string propertyId,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var property = $"properties/{propertyId}";
        var utcToday = DateTime.UtcNow.Date;
        var currentWeekStart = utcToday
            .AddDays(-(((int)utcToday.DayOfWeek + 6) % 7))
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var summaryTask = client.RunReportAsync(new RunReportRequest
        {
            Property = property,
            DateRanges = { new DateRange { StartDate = currentWeekStart, EndDate = "today" } },
            Metrics =
            {
                new Metric { Name = "sessions" },
                new Metric { Name = "screenPageViews" },
                new Metric { Name = "activeUsers" },
            },
        }, cancellationToken);

        var topPagesTask = client.RunReportAsync(new RunReportRequest
        {
            Property = property,
            DateRanges = { new DateRange { StartDate = "6daysAgo", EndDate = "today" } },
            Dimensions = { new Dimension { Name = "pagePath" } },
            Metrics = { new Metric { Name = "screenPageViews" } },
            Limit = 5,
            OrderBys =
            {
                new OrderBy
                {
                    Metric = new OrderBy.Types.MetricOrderBy { MetricName = "screenPageViews" },
                    Desc = true,
                },
            },
        }, cancellationToken);

        var dailySessionsTask = client.RunReportAsync(new RunReportRequest
        {
            Property = property,
            DateRanges = { new DateRange { StartDate = "29daysAgo", EndDate = "today" } },
            Dimensions = { new Dimension { Name = "date" } },
            Metrics = { new Metric { Name = "sessions" } },
            OrderBys =
            {
                new OrderBy
                {
                    Dimension = new OrderBy.Types.DimensionOrderBy { DimensionName = "date" },
                },
            },
        }, cancellationToken);

        await Task.WhenAll(summaryTask, topPagesTask, dailySessionsTask);

        var summary = await summaryTask;
        var topPages = await topPagesTask;
        var dailySessions = await dailySessionsTask;

        return new GoogleAnalyticsTrafficSnapshot(
            IsAvailable: true,
            SessionsLast7Days: GetMetricValue(summary, 0, 0),
            PageViewsLast7Days: GetMetricValue(summary, 0, 1),
            ActiveUsersLast7Days: GetMetricValue(summary, 0, 2),
            TopPagesThisWeek: topPages.Rows
                .Select(row => new GoogleAnalyticsTopPage(
                    row.DimensionValues.ElementAtOrDefault(0)?.Value ?? "/",
                    ParseInt(row.MetricValues.ElementAtOrDefault(0)?.Value)))
                .Where(page => page.Views > 0)
                .ToList(),
            DailySessionsLast30Days: dailySessions.Rows
                .Select(row => new GoogleAnalyticsDailySession(
                    ParseGaDate(row.DimensionValues.ElementAtOrDefault(0)?.Value),
                    ParseInt(row.MetricValues.ElementAtOrDefault(0)?.Value)))
                .ToList());
    }

    private BetaAnalyticsDataClient CreateClient()
    {
        var serviceAccountJson = options.Value.GoogleAnalyticsServiceAccountJson;
        if (string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            throw new InvalidOperationException("Google Analytics service account key is not configured.");
        }

        var credential = CredentialFactory
            .FromJson<ServiceAccountCredential>(serviceAccountJson)
            .ToGoogleCredential();
        return new BetaAnalyticsDataClientBuilder
        {
            Credential = credential,
        }.Build();
    }

    private static int GetMetricValue(RunReportResponse response, int rowIndex, int metricIndex) =>
        ParseInt(response.Rows.ElementAtOrDefault(rowIndex)?.MetricValues.ElementAtOrDefault(metricIndex)?.Value);

    private static int ParseInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : 0;

    private static DateOnly ParseGaDate(string? value) =>
        value is { Length: 8 }
            && int.TryParse(value[..4], out var year)
            && int.TryParse(value[4..6], out var month)
            && int.TryParse(value[6..8], out var day)
                ? new DateOnly(year, month, day)
                : DateOnly.MinValue;
}
