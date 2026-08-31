using System.Diagnostics;
using QueenZone.Web.Health;

namespace QueenZone.Web;

public sealed class RequestLogScopeMiddleware(RequestDelegate next, ILogger<RequestLogScopeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (QueenZoneHealthEndpoints.IsProbePath(context.Request.Path))
        {
            await next(context);
            return;
        }

        var state = new Dictionary<string, object?>
        {
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
        };
        var memberId = ForumMember.GetMemberId(context.User);
        if (memberId is Guid id)
        {
            state["MemberId"] = id.ToString("D");
        }

        using (logger.BeginScope(state))
        {
            await next(context);
        }
    }
}

public static class RequestLogScopeExtensions
{
    public static IApplicationBuilder UseRequestLogScope(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RequestLogScopeMiddleware>();
    }
}
