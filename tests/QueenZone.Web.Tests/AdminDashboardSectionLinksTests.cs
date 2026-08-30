using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminDashboardSectionLinksTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public AdminDashboardSectionLinksTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminDashboard_RendersLinksToAllAdminSections()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Admin sections", body);
        Assert.Contains("href=\"/admin/news\"", body);
        Assert.Contains("href=\"/admin/news-discovery\"", body);
        Assert.Contains("href=\"/admin/photos\"", body);
        Assert.Contains("href=\"/admin/biography\"", body);
        Assert.Contains("href=\"/admin/quotes\"", body);
        Assert.Contains("href=\"/admin/trivia\"", body);
        Assert.Contains("href=\"/admin/timeline\"", body);
        Assert.Contains("href=\"/admin/freddie-tributes\"", body);
        Assert.Contains("href=\"/admin/photo-submissions\"", body);
        Assert.Contains("href=\"/admin/news-suggestions\"", body);
        Assert.Contains("href=\"/admin/articles\"", body);
        Assert.Contains("href=\"/admin/help\"", body);
        Assert.Contains("href=\"/admin/private-messages\"", body);
    }
}
