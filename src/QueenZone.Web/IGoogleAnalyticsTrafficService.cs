namespace QueenZone.Web;

public interface IGoogleAnalyticsTrafficService
{
    Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(CancellationToken cancellationToken = default);
}

