using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages;

public sealed class ErrorModel : PageModel
{
    public int HttpStatusCodeValue { get; private set; }

    public string Eyebrow { get; private set; } = "Error";

    public string Heading { get; private set; } = "Something went wrong";

    public string Standfirst { get; private set; } =
        "We hit a problem while loading this page. The archive is still here, so try again or head back to a main section.";

    public string ActionHeading { get; private set; } = "Keep browsing the archive";

    public string? RequestId { get; private set; }

    public void OnGet(int? statusCode = null)
    {
        HttpStatusCodeValue = ResolveStatusCode(statusCode);
        Response.StatusCode = HttpStatusCodeValue;
        RequestId = HttpContext.TraceIdentifier;

        if (HttpStatusCodeValue == StatusCodes.Status404NotFound)
        {
            Eyebrow = "404";
            Heading = "Page Not Found";
            Standfirst = "Sorry, we couldn't find what you were looking for. It may have moved as the archive has been restored.";
            ActionHeading = "Find it in the archive";
            return;
        }

        if (HttpStatusCodeValue == StatusCodes.Status403Forbidden)
        {
            Eyebrow = "403";
            Heading = "Access denied";
            Standfirst = "This area is not available for the current account.";
            return;
        }

        if (HttpStatusCodeValue == StatusCodes.Status401Unauthorized)
        {
            Eyebrow = "401";
            Heading = "Sign in required";
            Standfirst = "This page needs an authenticated account before it can be viewed.";
            return;
        }

        Eyebrow = HttpStatusCodeValue == StatusCodes.Status500InternalServerError
            && HttpContext.Features.Get<IExceptionHandlerPathFeature>() is not null
                ? "500"
                : HttpStatusCodeValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private int ResolveStatusCode(int? statusCode)
    {
        if (IsErrorStatusCode(statusCode))
        {
            return statusCode.GetValueOrDefault();
        }

        var reExecuteStatusCode = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalStatusCode;
        if (IsErrorStatusCode(reExecuteStatusCode))
        {
            return reExecuteStatusCode.GetValueOrDefault();
        }

        if (IsErrorStatusCode(Response.StatusCode))
        {
            return Response.StatusCode;
        }

        return StatusCodes.Status500InternalServerError;
    }

    private static bool IsErrorStatusCode(int? statusCode) =>
        statusCode is >= StatusCodes.Status400BadRequest and < 600;
}
