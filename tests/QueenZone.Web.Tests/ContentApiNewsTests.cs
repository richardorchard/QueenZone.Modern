using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ContentApiNewsTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiNewsTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task News_list_requires_no_auth_and_returns_paged_response()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(5, payload.PageSize);
        Assert.Equal(5, payload.Items.Count);
        Assert.True(payload.TotalCount >= payload.Items.Count);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));
        Assert.All(payload.Items, item => Assert.Null(item.ImageUrl));
        Assert.All(payload.Items, item => Assert.Null(item.ThumbnailUrl));
    }

    [Fact]
    public async Task News_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
    }

    [Fact]
    public async Task News_detail_returns_published_article_body()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/1003");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<NewsDetailDto>();
        Assert.NotNull(item);
        Assert.Equal(1003, item!.Id);
        Assert.Equal("QueenZone modernisation begins", item.Title);
        Assert.False(string.IsNullOrWhiteSpace(item.Body));
        // Sanitized HTML parity with the website (#728): formatting, links, UGC images.
        Assert.Contains("<strong>ASP.NET Core</strong>", item.Body);
        Assert.Contains("href=\"https://www.queenzone.org/news\"", item.Body);
        Assert.Contains("src=\"/ugc/news/sample-crest.jpg\"", item.Body);
        Assert.DoesNotContain("<script", item.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToNewsDetail_sanitizes_body_like_website_FormatBody()
    {
        var item = new QueenZone.Data.NewsItem(
            42,
            "Title",
            "Excerpt",
            "<script>alert(1)</script><p>Hello <em>world</em></p><iframe src=\"https://evil.example\"></iframe>",
            new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            null,
            true);

        var dto = ContentApiMapper.ToNewsDetail(item);

        Assert.DoesNotContain("script", dto.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("iframe", dto.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<em>world</em>", dto.Body);
        Assert.Null(dto.ImageUrl);
        Assert.Null(dto.ThumbnailUrl);
    }

    [Fact]
    public void ToNewsListItem_and_ToNewsDetail_resolve_articles_blob_urls()
    {
        var item = new NewsItem(
            42,
            "Title",
            "Excerpt",
            "Body",
            new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            null,
            true,
            ImageBlobKey: "editors/me/hero.webp");

        var list = ContentApiMapper.ToNewsListItem(item);
        Assert.Equal("/ugc/articles/editors/me/hero.webp", list.ImageUrl);
        Assert.Equal("/ugc/articles/editors/me/hero.webp?size=thumb", list.ThumbnailUrl);

        var detail = ContentApiMapper.ToNewsDetail(item);
        Assert.Equal(list.ImageUrl, detail.ImageUrl);
        Assert.Equal(list.ThumbnailUrl, detail.ThumbnailUrl);

        var galleryOnly = item with { ImageBlobKey = "gallery:3120", ImageGalleryPicId = 3120 };
        var galleryList = ContentApiMapper.ToNewsListItem(galleryOnly);
        Assert.Null(galleryList.ImageUrl);
        Assert.Null(galleryList.ThumbnailUrl);
    }

    [Fact]
    public async Task News_detail_returns_problem_details_for_unpublished_or_missing_article()
    {
        using var client = factory.CreateAnonymousClient();

        using var hidden = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/9001");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("application/problem+json", hidden.Content.Headers.ContentType?.MediaType);
        var hiddenProblem = await hidden.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, hiddenProblem.GetProperty("status").GetInt32());

        using var missing = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/424242");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task News_list_decade_filter_finds_article_absent_from_unfiltered_first_page()
    {
        // 25 recent (2020s) articles plus one 2008 article — an unfiltered default page (size 20)
        // never reaches the 2008 article, so a naive client-side decade filter over that page
        // would incorrectly report "no articles" for the 2000s (issue #838).
        var items = new List<NewsItem>();
        for (var i = 0; i < 25; i++)
        {
            items.Add(new NewsItem(
                2000 + i,
                $"2020s article {i}",
                "Excerpt",
                "Body",
                new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-i),
                null,
                true));
        }

        items.Add(new NewsItem(
            9999,
            "Old article from the 2000s",
            "Excerpt",
            "Body",
            new DateTime(2008, 3, 4, 0, 0, 0, DateTimeKind.Utc),
            null,
            true));

        using var appFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<INewsRepository>();
            services.AddSingleton<INewsRepository>(_ => new FixedNewsRepository(items));
        });

        using var client = appFactory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?decade=2000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.TotalCount);
        Assert.Single(payload.Items);
        Assert.Equal(9999, payload.Items[0].Id);
    }

    [Fact]
    public async Task News_list_decade_filter_with_no_matches_returns_empty_page_not_error()
    {
        using var appFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<INewsRepository>();
            services.AddSingleton<INewsRepository>(_ => new FixedNewsRepository([
                new NewsItem(1, "Only 2026 article", "Ex", "Body", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true),
            ]));
        });

        using var client = appFactory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?decade=1990");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(0, payload!.TotalCount);
        Assert.Empty(payload.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9995)]
    public async Task News_list_out_of_range_decade_does_not_return_server_error(int decade)
    {
        using var client = factory.CreateAnonymousClient();

        using var unfiltered = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news");
        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?decade={decade}");

        Assert.Equal(HttpStatusCode.OK, unfiltered.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        var unfilteredPayload = await unfiltered.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(unfilteredPayload!.TotalCount, payload!.TotalCount);
        Assert.True(payload.TotalCount > 0);
    }

    [Fact]
    public async Task News_list_year_filter_finds_article_absent_from_unfiltered_first_page()
    {
        // Same setup as the decade-filter test, but the year-rail scrubber (issue #886) needs a
        // single-year window rather than a 10-year one.
        var items = new List<NewsItem>();
        for (var i = 0; i < 25; i++)
        {
            items.Add(new NewsItem(
                2000 + i,
                $"2020s article {i}",
                "Excerpt",
                "Body",
                new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-i),
                null,
                true));
        }

        items.Add(new NewsItem(
            9999,
            "Old article from 2008",
            "Excerpt",
            "Body",
            new DateTime(2008, 3, 4, 0, 0, 0, DateTimeKind.Utc),
            null,
            true));

        using var appFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<INewsRepository>();
            services.AddSingleton<INewsRepository>(_ => new FixedNewsRepository(items));
        });

        using var client = appFactory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?year=2008");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.TotalCount);
        Assert.Single(payload.Items);
        Assert.Equal(9999, payload.Items[0].Id);
    }

    [Fact]
    public async Task News_list_year_filter_wins_when_decade_is_also_supplied()
    {
        using var appFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<INewsRepository>();
            services.AddSingleton<INewsRepository>(_ => new FixedNewsRepository([
                new NewsItem(1, "2008 article", "Ex", "Body", new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true),
                new NewsItem(2, "2015 article", "Ex", "Body", new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true),
            ]));
        });

        using var client = appFactory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?decade=2010&year=2008");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.TotalCount);
        Assert.Equal(1, payload.Items[0].Id);
    }

    [Fact]
    public async Task News_years_returns_min_and_max_published_year()
    {
        using var appFactory = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<INewsRepository>();
            services.AddSingleton<INewsRepository>(_ => new FixedNewsRepository([
                new NewsItem(1, "Oldest", "Ex", "Body", new DateTime(2006, 5, 1, 0, 0, 0, DateTimeKind.Utc), null, true),
                new NewsItem(2, "Newest", "Ex", "Body", new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc), null, true),
                new NewsItem(3, "Hidden", "Ex", "Body", new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, false),
            ]));
        });

        using var client = appFactory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/years");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<NewsYearRangeDto>();
        Assert.NotNull(payload);
        Assert.Equal(2006, payload!.MinYear);
        Assert.Equal(2026, payload.MaxYear);
    }
}
