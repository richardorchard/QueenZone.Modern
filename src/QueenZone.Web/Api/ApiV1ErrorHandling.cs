using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace QueenZone.Web;

/// <summary>
/// JSON Problem Details for <c>/api/v1</c> so the mobile client never receives the HTML error pages.
/// OAuth token/authorize responses that already wrote a body are left untouched.
/// </summary>
public static class ApiV1ErrorHandling
{
    public static IApplicationBuilder UseApiV1ExceptionHandler(this IApplicationBuilder app)
    {
        app.UseWhen(
            static context => ApiV1.IsApiPath(context.Request.Path),
            branch => branch.UseExceptionHandler(errorApp => errorApp.Run(WriteUnhandledExceptionAsync)));
        return app;
    }

    public static IApplicationBuilder UseApiV1StatusCodePages(this IApplicationBuilder app)
    {
        app.UseWhen(
            static context => ApiV1.IsApiPath(context.Request.Path),
            branch => branch.Use(WriteProblemDetailsForEmptyErrorsAsync));
        return app;
    }

    internal static async Task WriteUnhandledExceptionAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("QueenZone.Web.Api");
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is not null)
        {
            logger.LogError(error, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.")
            .ExecuteAsync(context);
    }

    internal static async Task WriteProblemDetailsForEmptyErrorsAsync(HttpContext http, RequestDelegate next)
    {
        await next(http);
        await WriteStatusCodeProblemAsync(http);
    }

    internal static async Task WriteStatusCodeProblemAsync(HttpContext http)
    {
        if (http.Response.StatusCode is < 400 or > 599
            || http.Response.HasStarted
            || !string.IsNullOrEmpty(http.Response.ContentType))
        {
            return;
        }

        http.Response.ContentLength = null;
        await Results.Problem(
                statusCode: http.Response.StatusCode,
                title: ReasonPhrases.GetReasonPhrase(http.Response.StatusCode))
            .ExecuteAsync(http);
    }
}
