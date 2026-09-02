using System.IO;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Admin.Articles;

/// <summary>
/// Maps framework-before-save 400s (form field length, antiforgery, bind) on the
/// articles editor POST to a 200 with in-page errors, and logs the reason.
/// </summary>
internal sealed class EditorialArticleEditorRequestGuardFilter(ILogger<EditorialArticleEditorRequestGuardFilter> logger)
    : IAsyncPageFilter, IAsyncAlwaysRunResultFilter
{
    internal const string SizeError =
        "This draft is too large to save. Shorten the article body or attach a smaller cropped image.";

    internal const string AntiforgeryError =
        "This save could not be verified. Reload the page and try again.";

    internal const string BindError =
        "Some fields could not be read. Check the form and try again.";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HandlerInstance is EditModel page
            && HttpMethods.IsPost(context.HttpContext.Request.Method)
            && !context.ModelState.IsValid)
        {
            AddModelStateErrors(page, context.ModelState);
            logger.LogWarning(
                "Articles editor POST bind failed: {Keys}",
                string.Join(", ", InvalidModelStateKeys(context.ModelState)));
        }

        await next();
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Controller is EditModel page && IsBadRequest(context))
        {
            var (message, reason) = Describe(context.HttpContext, page);
            await FailAsync(page, message, reason, context.HttpContext.RequestAborted);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.Result = new PageResult();
        }

        await next();
    }

    private static bool IsBadRequest(ResultExecutingContext context) =>
        context.Result is IStatusCodeActionResult { StatusCode: StatusCodes.Status400BadRequest }
        || context.HttpContext.Response.StatusCode == StatusCodes.Status400BadRequest;

    internal static bool IsHandled(Exception exception) =>
        exception is BadHttpRequestException or InvalidDataException or AntiforgeryValidationException;

    internal static string MessageFor(Exception exception) => exception switch
    {
        AntiforgeryValidationException => AntiforgeryError,
        BadHttpRequestException or InvalidDataException => SizeError,
        _ => BindError,
    };

    private async Task FailAsync(EditModel page, string message, string reason, CancellationToken cancellationToken)
    {
        logger.LogWarning("Articles editor POST rejected: {Reason}", reason);
        page.AddError(message);
        await page.PrepareRedisplayAsync(cancellationToken);
    }

    private static (string Message, string Reason) Describe(HttpContext httpContext, EditModel page)
    {
        var antiforgery = httpContext.Features.Get<IAntiforgeryValidationFeature>();
        if (antiforgery is { IsValid: false })
        {
            return (AntiforgeryError, antiforgery.Error?.Message ?? "antiforgery");
        }

        if (!page.ModelState.IsValid)
        {
            AddModelStateErrors(page, page.ModelState);
            return (BindError, string.Join(", ", InvalidModelStateKeys(page.ModelState)));
        }

        return (SizeError, "request-form-or-size");
    }

    private static void AddModelStateErrors(EditModel page, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        foreach (var entry in modelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                var message = string.IsNullOrWhiteSpace(error.ErrorMessage) ? BindError : error.ErrorMessage;
                page.AddError(message);
            }
        }
    }

    private static IEnumerable<string> InvalidModelStateKeys(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
        modelState.Where(entry => entry.Value is { Errors.Count: > 0 }).Select(entry => entry.Key);
}
