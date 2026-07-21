using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminDashboardSectionLinksTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public AdminDashboardSectionLinksTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task AdminDashboard_RendersLinksToAllAdminSections()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Email", AdminEmail);

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Admin sections", body);
        Assert.Contains("href=\"/admin/news\"", body);
        Assert.Contains("href=\"/admin/news-discovery\"", body);
        Assert.Contains("href=\"/admin/photo-submissions\"", body);
        Assert.Contains("href=\"/admin/news-suggestions\"", body);
        Assert.Contains("href=\"/admin/articles\"", body);
    }
}
