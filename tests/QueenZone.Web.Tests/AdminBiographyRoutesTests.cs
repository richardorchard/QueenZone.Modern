using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminBiographyRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public AdminBiographyRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AnonymousUserCannotAccessAdminBiography()
    {
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);

        var response = await client.GetAsync("/admin/biography");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedNonAdminCannotAccessAdminBiography()
    {
        var client = factory.CreateAdminClient("stranger@example.com");

        var response = await client.GetAsync("/admin/biography");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedAdminCanListSeedChapters()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/admin/biography");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Biography chapters", body);
        Assert.Contains("/admin/biography/new", body);
        Assert.Contains("1946 - 1969", body);
        Assert.Contains("/admin/biography/1/edit", body);
    }

    [Fact]
    public async Task AuthorizedAdminCanCreateAndEditChapterWithRichText()
    {
        var store = new SharedBiographyStore();
        var client = CreateClient(store);

        var createResponse = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/biography/new",
            "/admin/biography",
            new Dictionary<string, string>
            {
                ["title"] = "1991",
                ["summary"] = "A closing chapter.",
                ["body"] = "<p><strong>Bold biography</strong> text.</p>",
                ["displaySequence"] = "9"
            });

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var editPath = createResponse.Headers.Location!.OriginalString;
        Assert.Matches(@"/admin/biography/\d+/edit", editPath);
        var chapterId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);

        var editGet = await client.GetStringAsync(editPath);
        Assert.Contains("1991", editGet);
        Assert.Contains("Bold biography", editGet);
        Assert.Contains("rich-text-editor", editGet, StringComparison.OrdinalIgnoreCase);

        var saveResponse = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            editPath,
            $"/admin/biography/{chapterId}",
            new Dictionary<string, string>
            {
                ["title"] = "1991 revised",
                ["summary"] = "Updated summary.",
                ["body"] = "<p>Updated <em>chapter</em> body.</p>",
                ["displaySequence"] = "10"
            });

        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);
        Assert.Equal($"/admin/biography/{chapterId}/edit", saveResponse.Headers.Location!.OriginalString);

        var updated = await client.GetStringAsync($"/admin/biography/{chapterId}/edit");
        Assert.Contains("1991 revised", updated);
        Assert.Contains("Updated summary.", updated);
        Assert.Contains("Updated", updated);

        var publicBody = await client.GetStringAsync($"/biography/{chapterId}/1991-revised");
        Assert.Contains("Updated", publicBody);
        Assert.Contains("<em>chapter</em>", publicBody);
    }

    [Fact]
    public async Task ValidationFailuresAreReturnedForInvalidChapter()
    {
        var store = new SharedBiographyStore();
        var client = CreateClient(store);

        var response = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/biography/new",
            "/admin/biography",
            new Dictionary<string, string>
            {
                ["title"] = "",
                ["summary"] = new string('x', BiographyValidation.MaxSummaryLength + 1),
                ["body"] = "<p><br></p>",
                ["displaySequence"] = "0"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", body);
        Assert.Contains($"Summary must be {BiographyValidation.MaxSummaryLength} characters or fewer.", body);
        Assert.Contains("Chapter body is required.", body);
        Assert.Contains("Display sequence must be between 1 and 255.", body);
    }

    [Fact]
    public async Task EditValidationFailuresAreReturnedOnPost()
    {
        var store = new SharedBiographyStore(
        [
            new BiographyChapterItem(8, "1977", "Summary", "<p>Body</p>", 1, DateTime.UtcNow)
        ]);
        var client = CreateClient(store);

        var response = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/biography/8/edit",
            "/admin/biography/8",
            new Dictionary<string, string>
            {
                ["title"] = "",
                ["summary"] = "",
                ["body"] = "<p><br></p>",
                ["displaySequence"] = "0"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", body);
        Assert.Contains("Chapter body is required.", body);
        Assert.Contains("Display sequence must be between 1 and 255.", body);
    }

    [Fact]
    public async Task EditMissingChapterReturnsNotFound()
    {
        var store = new SharedBiographyStore();
        var client = CreateClient(store);

        var response = await client.GetAsync("/admin/biography/9999/edit");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditPostMissingChapterReturnsNotFound()
    {
        var store = new SharedBiographyStore();
        var client = CreateClient(store);

        var response = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/biography/new",
            "/admin/biography/9999",
            new Dictionary<string, string>
            {
                ["title"] = "Ghost",
                ["summary"] = "",
                ["body"] = "<p>Body</p>",
                ["displaySequence"] = "1"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HtmlIsSanitizedOnSave()
    {
        var store = new SharedBiographyStore();
        var client = CreateClient(store);

        var createResponse = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/biography/new",
            "/admin/biography",
            new Dictionary<string, string>
            {
                ["title"] = "Safe chapter",
                ["summary"] = "",
                ["body"] = "<p>Hello</p><script>alert(1)</script>",
                ["displaySequence"] = "1"
            });

        var chapterId = AdminHttpTestHelpers.ParseNewsIdFromEditRedirect(createResponse);
        var chapter = store.GetById(chapterId);
        Assert.NotNull(chapter);
        Assert.DoesNotContain("<script>", chapter.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Hello</p>", chapter.Body);
    }

    private HttpClient CreateClient(SharedBiographyStore store)
    {
        var appFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SharedBiographyStore>();
                services.RemoveAll<IBiographyRepository>();
                services.AddSingleton(store);
                services.AddSingleton<IBiographyRepository>(_ => new InMemoryBiographyRepository(store));
            }));

        return AdminHttpTestHelpers.CreateClient(appFactory, AdminHttpTestHelpers.AdminEmail);
    }
}
