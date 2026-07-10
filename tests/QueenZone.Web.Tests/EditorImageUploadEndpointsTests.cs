using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class EditorImageUploadEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public EditorImageUploadEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task UploadAsync_returns_unauthorized_when_not_authenticated()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
            RequestServices = factory.Services,
        };
        var antiforgery = factory.Services.GetRequiredService<IAntiforgery>();

        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file: null,
            container: null,
            new StubBlobUploadService(),
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status401Unauthorized, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task UploadAsync_returns_bad_request_for_missing_file()
    {
        var (httpContext, antiforgery) = CreateAuthenticatedHttpContext();
        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file: null,
            container: null,
            new StubBlobUploadService(),
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task UploadAsync_returns_bad_request_for_non_image()
    {
        var (httpContext, antiforgery) = CreateAuthenticatedHttpContext();
        var file = CreateFormFile("note.txt", "text/plain", "hello"u8.ToArray());
        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file,
            container: null,
            new StubBlobUploadService(),
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task UploadAsync_returns_bad_request_for_oversized_image()
    {
        var (httpContext, antiforgery) = CreateAuthenticatedHttpContext();
        var huge = new byte[EditorImageUploadEndpoints.MaxImageBytes + 1];
        huge[0] = 0xFF;
        huge[1] = 0xD8;
        huge[2] = 0xFF;
        var file = CreateFormFile("big.jpg", "image/jpeg", huge);
        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file,
            container: null,
            new StubBlobUploadService(),
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task UploadAsync_returns_bad_request_for_disallowed_container()
    {
        var (httpContext, antiforgery) = CreateAuthenticatedHttpContext();
        var file = CreateFormFile("x.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);
        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file,
            container: "legacy-photos",
            new StubBlobUploadService(),
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task UploadAsync_returns_service_unavailable_when_storage_not_configured()
    {
        var (httpContext, antiforgery) = CreateAuthenticatedHttpContext();
        var file = CreateFormFile("x.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);
        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file,
            container: null,
            new NullBlobUploadService(),
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, await GetStatusCodeAsync(result));
    }

    [Fact]
    public async Task UploadAsync_returns_ok_with_url_when_storage_succeeds()
    {
        var (httpContext, antiforgery) = CreateAuthenticatedHttpContext();
        var stub = new StubBlobUploadService();
        var file = CreateFormFile("x.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);
        var result = await EditorImageUploadEndpoints.UploadAsync(
            httpContext,
            file,
            container: BlobUploadContainers.Articles,
            stub,
            antiforgery,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, await GetStatusCodeAsync(result));
        Assert.Equal(BlobUploadContainers.Articles, stub.LastContainer);
    }

    [Fact]
    public async Task Http_route_rejects_anonymous_caller()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var content = CreateJpegForm();
        var response = await client.PostAsync(EditorImageUploadEndpoints.Route, content);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Http_route_returns_ok_when_authenticated_with_stub_storage()
    {
        var stub = new StubBlobUploadService();
        await using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services => services.AddSingleton<IBlobUploadService>(stub));
        });

        var client = custom.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, AdminHttpTestHelpers.AdminEmail);

        var token = await GetAntiforgeryTokenAsync(client);
        using var form = CreateJpegForm(token);
        var response = await client.PostAsync(EditorImageUploadEndpoints.Route, form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://cdn.test/ugc-forum/editors/x.jpg", doc.RootElement.GetProperty("url").GetString());
        Assert.Equal(BlobUploadContainers.Forum, stub.LastContainer);
    }

    private (HttpContext HttpContext, IAntiforgery Antiforgery) CreateAuthenticatedHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Email, AdminHttpTestHelpers.AdminEmail),
                    new Claim(ClaimTypes.Name, AdminHttpTestHelpers.AdminEmail),
                ],
                authenticationType: "Test")),
            RequestServices = factory.Services,
        };
        httpContext.Request.Method = "POST";
        return (httpContext, new NoopAntiforgery());
    }

    /// <summary>Skips real antiforgery crypto for direct UploadAsync unit tests.</summary>
    private sealed class NoopAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) =>
            new("request", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) =>
            new("request", "cookie", "form", "header");

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
        }

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/admin/news");
        return AdminHttpTestHelpers.ExtractAntiforgeryToken(html);
    }

    private static MultipartFormDataContent CreateJpegForm(string? token = null)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0xFF, 0xD9]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "paste.jpg");
        if (!string.IsNullOrEmpty(token))
        {
            content.Add(new StringContent(token), "__RequestVerificationToken");
        }

        return content;
    }

    private static IFormFile CreateFormFile(string name, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private async Task<int> GetStatusCodeAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext { RequestServices = factory.Services };
        await result.ExecuteAsync(httpContext);
        return httpContext.Response.StatusCode;
    }

    private sealed class StubBlobUploadService : IBlobUploadService
    {
        public string? LastContainer { get; private set; }

        public Task<BlobUploadResult> UploadAsync(
            Stream content,
            string originalFileName,
            string containerName,
            BlobUploadContext? context = null,
            CancellationToken cancellationToken = default)
        {
            LastContainer = containerName;
            return Task.FromResult(new BlobUploadResult
            {
                Container = containerName,
                BlobName = "editors/x.jpg",
                ContentType = "image/jpeg",
                SizeBytes = 8,
                PublicUrl = "https://cdn.test/ugc-forum/editors/x.jpg",
            });
        }

        public Task DeleteAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
