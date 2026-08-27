using System.Net;

namespace QueenZone.Web.Tests;

public sealed class AdminQuotesRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public AdminQuotesRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AnonymousUserCannotAccessAdminQuotes()
    {
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);

        var response = await client.GetAsync("/admin/quotes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedAdminCanListSeedQuotes()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/admin/quotes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Quotes", body);
        Assert.Contains("/admin/quotes/new", body);
        Assert.Contains("Freddie Mercury", body);
        Assert.Contains("/admin/quotes/1/edit", body);
    }

    [Fact]
    public async Task AuthorizedAdminCanOpenNewQuoteForm()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/admin/quotes/new");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Add quote", body);
        Assert.Contains("Who said it", body);
    }

    [Fact]
    public async Task AuthorizedAdminCanOpenEditFormForSeedQuote()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/admin/quotes/1/edit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Edit quote", body);
        Assert.Contains("Freddie Mercury", body);
    }

    [Fact]
    public async Task AuthorizedAdminGetsNotFoundForMissingQuote()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/admin/quotes/99999/edit");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
