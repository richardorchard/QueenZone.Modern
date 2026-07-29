using QueenZone.Data;

namespace QueenZone.Web;

public static class FanPerformanceEndpoints
{
    public static void MapFanPerformanceEndpoints(this WebApplication app)
    {
        app.MapGet("/fan-performances/{id:int}/audio/{filename?}", async (
            int id,
            IFanPerformanceRepository fanPerformanceRepository,
            CancellationToken cancellationToken) =>
        {
            var performance = await fanPerformanceRepository.GetByIdAsync(id, cancellationToken);
            return performance is null
                ? Results.NotFound()
                : Results.Redirect(performance.AudioUrl);
        })
        .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy)
        .RequireRateLimiting(FanPerformanceRateLimitingOptions.AudioPolicy);
    }
}
