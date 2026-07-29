namespace QueenZone.Web;

internal interface IGoogleAnalyticsDataClient
{
    Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(
        string propertyId,
        CancellationToken cancellationToken = default);
}

