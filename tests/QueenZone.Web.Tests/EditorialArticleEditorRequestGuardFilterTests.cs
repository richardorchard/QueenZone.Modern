using System.IO;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Web.Pages.Admin.Articles;

namespace QueenZone.Web.Tests;

public sealed class EditorialArticleEditorRequestGuardFilterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public EditorialArticleEditorRequestGuardFilterTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public void IsHandled_covers_size_and_antiforgery_exceptions()
    {
        Assert.True(EditorialArticleEditorRequestGuardFilter.IsHandled(new BadHttpRequestException("Failed to read the request form.")));
        Assert.True(EditorialArticleEditorRequestGuardFilter.IsHandled(new InvalidDataException("Form value length limit 4194304 exceeded.")));
        Assert.True(EditorialArticleEditorRequestGuardFilter.IsHandled(new AntiforgeryValidationException("The required antiforgery cookie is not present.")));
        Assert.False(EditorialArticleEditorRequestGuardFilter.IsHandled(new InvalidOperationException("other")));
    }

    [Fact]
    public void MessageFor_maps_size_and_antiforgery_to_in_page_copy()
    {
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.SizeError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new BadHttpRequestException("Request body too large.")));
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.SizeError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new InvalidDataException("Form value length limit 4194304 exceeded.")));
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.AntiforgeryError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new AntiforgeryValidationException("The required antiforgery cookie is not present.")));
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.BindError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new FormatException("bad date")));
    }

    [Fact]
    public async Task OnExceptionAsync_maps_size_failure_to_200_with_in_page_error()
    {
        using var scope = factory.Services.CreateScope();
        var (page, httpContext) = CreatePage(scope.ServiceProvider);
        var context = new ExceptionContext(page.PageContext, [])
        {
            Exception = new BadHttpRequestException("Failed to read the request form."),
        };

        await page.OnExceptionAsync(context);

        Assert.True(context.ExceptionHandled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.IsType<PageResult>(context.Result);
        Assert.Contains(EditorialArticleEditorRequestGuardFilter.SizeError, page.Errors);
    }

    [Fact]
    public async Task OnExceptionAsync_ignores_unrelated_exceptions()
    {
        using var scope = factory.Services.CreateScope();
        var (page, httpContext) = CreatePage(scope.ServiceProvider);
        var context = new ExceptionContext(page.PageContext, [])
        {
            Exception = new InvalidOperationException("slug already exists"),
        };

        await page.OnExceptionAsync(context);

        Assert.False(context.ExceptionHandled);
        Assert.Null(context.Result);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Empty(page.Errors);
    }

    [Fact]
    public async Task Result_filter_maps_400_to_200_with_size_copy()
    {
        using var scope = factory.Services.CreateScope();
        var (page, httpContext) = CreatePage(scope.ServiceProvider);
        var filter = new EditorialArticleEditorRequestGuardFilter(NullLogger<EditorialArticleEditorRequestGuardFilter>.Instance);
        var context = new ResultExecutingContext(
            page.PageContext,
            [],
            new BadRequestResult(),
            page);

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(new ResultExecutedContext(page.PageContext, [], context.Result, page)));

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.IsType<PageResult>(context.Result);
        Assert.Contains(EditorialArticleEditorRequestGuardFilter.SizeError, page.Errors);
    }

    [Fact]
    public async Task Result_filter_maps_antiforgery_feature_to_in_page_copy()
    {
        using var scope = factory.Services.CreateScope();
        var (page, httpContext) = CreatePage(scope.ServiceProvider);
        httpContext.Features.Set<IAntiforgeryValidationFeature>(new FailedAntiforgeryFeature());
        var filter = new EditorialArticleEditorRequestGuardFilter(NullLogger<EditorialArticleEditorRequestGuardFilter>.Instance);
        var context = new ResultExecutingContext(
            page.PageContext,
            [],
            new BadRequestResult(),
            page);

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(new ResultExecutedContext(page.PageContext, [], context.Result, page)));

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Contains(EditorialArticleEditorRequestGuardFilter.AntiforgeryError, page.Errors);
    }

    [Fact]
    public async Task Result_filter_maps_invalid_model_state_to_bind_copy()
    {
        using var scope = factory.Services.CreateScope();
        var (page, httpContext) = CreatePage(scope.ServiceProvider);
        page.ModelState.AddModelError("Form.PublishedAt", "The value 'not-a-date' is not valid.");
        var filter = new EditorialArticleEditorRequestGuardFilter(NullLogger<EditorialArticleEditorRequestGuardFilter>.Instance);
        var context = new ResultExecutingContext(
            page.PageContext,
            [],
            new StatusCodeResult(StatusCodes.Status400BadRequest),
            page);

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(new ResultExecutedContext(page.PageContext, [], context.Result, page)));

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Contains("The value 'not-a-date' is not valid.", page.Errors);
    }

    [Fact]
    public async Task Admin_save_draft_over_value_length_limit_returns_200_with_in_page_error()
    {
        using var isolated = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.Configure<FormOptions>(options => options.ValueLengthLimit = 2048);
            });
        });
        var admin = AdminHttpTestHelpers.CreateClient(isolated, AdminHttpTestHelpers.AdminEmail);
        var response = await AdminHttpTestHelpers.PostArticleAsync(
            admin,
            "/admin/articles/editor?legacyId=101",
            "/admin/articles/editor",
            new()
            {
                ["Form.LegacyArticleId"] = "101",
                ["Form.Title"] = "Oversized archive body",
                ["Form.Excerpt"] = "Should stay on the form.",
                ["Form.Body"] = "<p>" + new string('x', 8192) + "</p>",
                ["Form.AuthorName"] = "Archive Editor",
                ["Form.Category"] = "Features",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual("/error/400", response.Headers.Location?.OriginalString);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("admin-errors", html);
        Assert.Contains(EditorialArticleEditorRequestGuardFilter.SizeError, html);
        Assert.DoesNotContain("Something went wrong", html);
    }

    private static (EditModel Page, DefaultHttpContext HttpContext) CreatePage(IServiceProvider services)
    {
        var page = ActivatorUtilities.CreateInstance<EditModel>(services);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        page.PageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), page.ModelState),
        };
        return (page, httpContext);
    }

    private sealed class FailedAntiforgeryFeature : IAntiforgeryValidationFeature
    {
        public bool IsValid => false;

        public Exception? Error { get; } = new InvalidOperationException("The required antiforgery cookie is not present.");
    }
}
