using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

/// <summary>
/// EF/SQLite HTTP coverage for /admin. Guarantees the dashboard does not parallelize
/// queries on the shared scoped DbContext (which 500s in production).
/// </summary>
public sealed class AdminDashboardEfRoutesTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private const string AdminEmail = "admin@test.local";

    private readonly WebApplicationFactory<Program> baseFactory;
    private readonly AdminEfWebTestHarness harness;
    private WebApplicationFactory<Program> factory = null!;

    public AdminDashboardEfRoutesTests(WebApplicationFactory<Program> baseFactory)
    {
        this.baseFactory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:QueenZoneLegacy", string.Empty);
        });
        harness = new AdminEfWebTestHarness();
    }

    public Task InitializeAsync()
    {
        factory = harness.CreateFactory(baseFactory, services =>
        {
            services.RemoveAll<IPhotoSubmissionRepository>();
            services.RemoveAll<INewsSuggestionRepository>();
            // IArticleSubmissionRepository / IArticleRepository are already removed by the harness.
            services.AddScoped<IMemberAccountRepository, EfMemberAccountRepository>();
            services.AddScoped<IPhotoSubmissionRepository, EfPhotoSubmissionRepository>();
            services.AddScoped<INewsSuggestionRepository, EfNewsSuggestionRepository>();
            services.AddScoped<IArticleSubmissionRepository, EfArticleSubmissionRepository>();
            services.AddScoped<IArticleRepository, EfArticleRepository>();
        });
        harness.EnsureSchema(factory.Services);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task AdminDashboard_EfBacked_ReturnsOk()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Test-User-Email", AdminEmail);

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Admin sections", body);
        Assert.Contains("Submission queue", body);
        Assert.DoesNotContain("A second operation was started on this context", body);
    }
}
