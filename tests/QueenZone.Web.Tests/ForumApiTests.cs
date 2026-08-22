using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class ForumApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ForumApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Categories_list_requires_no_auth_and_matches_website_boards()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumCategoryListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.DefaultPageSize, payload.PageSize);
        Assert.Equal(6, payload.TotalCount);
        Assert.Equal(6, payload.Items.Count);
        Assert.Equal("The Music", payload.Items[0].Name);
        Assert.Equal("/forum/1/the-music", payload.Items[0].DetailPath);
        Assert.Equal("The Lounge", payload.Items[^1].Name);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));

        var html = await client.GetStringAsync("/forum");
        Assert.All(payload.Items, item =>
        {
            // Razor HTML-encodes board names (`Live & Tours` → `Live &amp; Tours`).
            Assert.Contains(WebUtility.HtmlEncode(item.Name), html, StringComparison.Ordinal);
            Assert.Contains(item.DetailPath, html, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Categories_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumCategoryListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
        Assert.Equal(6, payload.Items.Count);
    }

    [Fact]
    public async Task Category_detail_returns_public_board()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ForumCategoryListItemDto>();
        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.Equal("The Music", item.Name);
        Assert.Equal("/forum/1/the-music", item.DetailPath);
        Assert.Equal("Ranking every studio album", item.LatestThreadTitle);
    }

    [Fact]
    public async Task Category_detail_returns_problem_details_for_missing_board()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
        Assert.Contains("9999", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Topics_list_matches_website_category_page_order_and_paging()
    {
        using var client = factory.CreateAnonymousClient();

        using var firstPageResponse = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics?page=1&pageSize={ForumRoutes.TopicsPageSize}");
        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(firstPage);
        Assert.Equal(1, firstPage!.Page);
        Assert.Equal(ForumRoutes.TopicsPageSize, firstPage.PageSize);
        Assert.Equal(30, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(ForumRoutes.TopicsPageSize, firstPage.Items.Count);
        Assert.Equal("Forum Guidelines", firstPage.Items[0].Title);
        Assert.True(firstPage.Items[0].IsSticky);
        Assert.Equal("/forum/topic/1001/forum-guidelines", firstPage.Items[0].DetailPath);
        Assert.Equal("Ranking every studio album", firstPage.Items[1].Title);
        Assert.False(firstPage.Items[1].IsSticky);

        var html = await client.GetStringAsync("/forum/1/the-music");
        Assert.Contains("Forum Guidelines", html, StringComparison.Ordinal);
        Assert.Contains(firstPage.Items[0].DetailPath, html, StringComparison.Ordinal);
        Assert.Contains(firstPage.Items[1].DetailPath, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Archive sample thread 1030", html, StringComparison.Ordinal);

        using var secondPageResponse = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics?page=2&pageSize={ForumRoutes.TopicsPageSize}");
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage!.Page);
        Assert.Contains(secondPage.Items, item => item.Title == "Archive sample thread 1030");

        var pageTwoHtml = await client.GetStringAsync("/forum/1/the-music/page/2");
        Assert.Contains("Archive sample thread 1030", pageTwoHtml, StringComparison.Ordinal);
        Assert.All(secondPage.Items, item => Assert.Contains(item.DetailPath, pageTwoHtml, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Topics_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
        Assert.Equal(30, payload.Items.Count);
    }

    [Fact]
    public async Task Topics_list_returns_empty_page_for_board_with_no_topics()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/2/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Items);
        Assert.Equal(0, payload.TotalCount);
        Assert.Equal(0, payload.TotalPages);
    }

    [Fact]
    public async Task Topics_list_returns_problem_details_for_missing_board()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/9999/topics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void Mapper_builds_same_canonical_paths_as_website()
    {
        var category = new ForumCategoryItem(
            1,
            "Queen - Serious Discussion",
            "Board",
            10,
            new DateTime(2024, 6, 12, 0, 0, 0, DateTimeKind.Utc),
            "Latest",
            10);
        var topic = new ForumTopicItem(
            1002,
            "Ranking every studio album",
            new DateTime(2024, 6, 12, 14, 0, 0, DateTimeKind.Utc),
            "brightonrock",
            1284,
            "brightonrock",
            false);

        var categoryDto = ForumApiMapper.ToCategoryListItem(category);
        var topicDto = ForumApiMapper.ToTopicListItem(topic);

        Assert.Equal("/forum/1/queen-serious-discussion", categoryDto.DetailPath);
        Assert.Equal("/forum/topic/1002/ranking-every-studio-album", topicDto.DetailPath);
        Assert.Equal(ForumApiMapper.ToCategoryListItems([category])[0], categoryDto);
        Assert.Equal(ForumApiMapper.ToTopicListItems([topic])[0], topicDto);

        var header = new ForumTopicHeader(1002, " Ranking every studio album ", 1, " The Music ");
        var topicDetail = ForumApiMapper.ToTopicDetail(header, postCount: 26);
        Assert.Equal(1002, topicDetail.Id);
        Assert.Equal("Ranking every studio album", topicDetail.Title);
        Assert.Equal("/forum/1/the-music", topicDetail.CategoryPath);
        Assert.Equal("/forum/topic/1002/ranking-every-studio-album", topicDetail.DetailPath);
        Assert.Equal(26, topicDetail.PostCount);
        Assert.False(topicDetail.IsLocked);
        Assert.True(ForumApiMapper.ToTopicDetail(header, postCount: 26, isLocked: true).IsLocked);

        var post = new ForumPostItem(
            1002,
            "Where would you put <strong>A Night at the Opera</strong> in the ranking?",
            new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            "brightonrock",
            "Queen collector since 1989.",
            4812,
            new DateTime(2004, 3, 12, 0, 0, 0, DateTimeKind.Utc),
            [new ForumPostAttachment(
                "anoto-setlist-scan.jpg",
                284_712,
                ForumAttachmentPaths.LegacyDownloadPath(1002))]);
        var ugcHtml = new UgcHtml(Options.Create(new BlobUploadOptions()));
        var postDto = ForumApiMapper.ToPost(post, ugcHtml);
        Assert.Contains("<strong>A Night at the Opera</strong>", postDto.Body, StringComparison.Ordinal);
        Assert.Equal("brightonrock", postDto.AuthorUsername);
        Assert.Single(postDto.Attachments);
        Assert.Equal("anoto-setlist-scan.jpg", postDto.Attachments[0].FileName);
        Assert.Equal("/forum/attachment/legacy/1002", postDto.Attachments[0].Url);
        Assert.Equal("JPG", postDto.Attachments[0].Extension);
        Assert.True(postDto.Attachments[0].IsImage);
        var mappedPosts = ForumApiMapper.ToPosts([post], ugcHtml);
        Assert.Equal(postDto.Id, mappedPosts[0].Id);
        Assert.Equal(postDto.Body, mappedPosts[0].Body);
        Assert.Equal(postDto.Attachments[0], mappedPosts[0].Attachments[0]);
    }

    [Fact]
    public async Task Topic_detail_requires_no_auth_and_matches_website_header()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var topic = await response.Content.ReadFromJsonAsync<ForumTopicDetailDto>();
        Assert.NotNull(topic);
        Assert.Equal(1002, topic!.Id);
        Assert.Equal("Ranking every studio album", topic.Title);
        Assert.Equal(1, topic.ForumId);
        Assert.Equal("The Music", topic.ForumName);
        Assert.Equal("/forum/1/the-music", topic.CategoryPath);
        Assert.Equal("/forum/topic/1002/ranking-every-studio-album", topic.DetailPath);
        Assert.Equal(26, topic.PostCount);
        Assert.False(topic.IsLocked);

        var html = await client.GetStringAsync(topic.DetailPath);
        Assert.Contains(WebUtility.HtmlEncode(topic.Title), html, StringComparison.Ordinal);
        Assert.Contains(WebUtility.HtmlEncode(topic.ForumName), html, StringComparison.Ordinal);
        Assert.Contains("<strong>26</strong> posts", html, StringComparison.Ordinal);
        Assert.Contains(topic.DetailPath, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Topic_detail_returns_problem_details_for_missing_thread()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
        Assert.Contains("9999", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Topic_posts_match_website_order_paging_and_attachments()
    {
        using var client = factory.CreateAnonymousClient();

        using var firstPageResponse = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/topics/1002/posts?page=1&pageSize={ForumRoutes.PostsPageSize}");
        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumPostDto>>();
        Assert.NotNull(firstPage);
        Assert.Equal(1, firstPage!.Page);
        Assert.Equal(ForumRoutes.PostsPageSize, firstPage.PageSize);
        Assert.Equal(26, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(ForumRoutes.PostsPageSize, firstPage.Items.Count);
        Assert.Equal(1002, firstPage.Items[0].Id);
        Assert.Equal("brightonrock", firstPage.Items[0].AuthorUsername);
        Assert.Contains("A Night at the Opera", firstPage.Items[0].Body, StringComparison.Ordinal);
        Assert.Single(firstPage.Items[0].Attachments);
        Assert.Equal("anoto-setlist-scan.jpg", firstPage.Items[0].Attachments[0].FileName);
        Assert.Equal("/forum/attachment/legacy/1002", firstPage.Items[0].Attachments[0].Url);
        Assert.Equal("JPG", firstPage.Items[0].Attachments[0].Extension);
        Assert.Equal("278.0 KB", firstPage.Items[0].Attachments[0].FormattedSize);
        Assert.True(firstPage.Items[0].Attachments[0].IsImage);
        Assert.DoesNotContain(firstPage.Items, item => item.Body.Contains("Archive reply 1125", StringComparison.Ordinal));
        var notes = firstPage.Items.Single(item => item.Id == 1101);
        Assert.Single(notes.Attachments);
        Assert.Equal("opera-side-two-notes.pdf", notes.Attachments[0].FileName);
        Assert.Equal("/forum/attachment/legacy/1101", notes.Attachments[0].Url);
        Assert.Equal("PDF", notes.Attachments[0].Extension);
        Assert.False(notes.Attachments[0].IsImage);

        var html = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album");
        Assert.Contains(WebUtility.HtmlEncode(firstPage.Items[0].AuthorUsername), html, StringComparison.Ordinal);
        Assert.Contains("A Night at the Opera", html, StringComparison.Ordinal);
        Assert.Contains("/forum/attachment/legacy/1002", html, StringComparison.Ordinal);
        Assert.Contains("anoto-setlist-scan.jpg", html, StringComparison.Ordinal);
        Assert.Contains("278.0 KB", html, StringComparison.Ordinal);
        Assert.Contains("opera-side-two-notes.pdf", html, StringComparison.Ordinal);
        Assert.Contains("/forum/attachment/legacy/1101", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Archive reply 1125", html, StringComparison.Ordinal);
        Assert.DoesNotContain("cdn.queenzone.org/attachments/", html, StringComparison.Ordinal);

        using var secondPageResponse = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/topics/1002/posts?page=2&pageSize={ForumRoutes.PostsPageSize}");
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumPostDto>>();
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage!.Page);
        Assert.Contains(secondPage.Items, item => item.Body.Contains("Archive reply 1125", StringComparison.Ordinal));

        var pageTwoHtml = await client.GetStringAsync("/forum/topic/1002/ranking-every-studio-album/page/2");
        Assert.Contains("Archive reply 1125", pageTwoHtml, StringComparison.Ordinal);
        Assert.All(
            secondPage.Items,
            item => Assert.Contains(WebUtility.HtmlEncode(item.AuthorUsername), pageTwoHtml, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Topic_posts_default_and_clamp_to_website_page_size()
    {
        using var client = factory.CreateAnonymousClient();

        using var omitted = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1002/posts");
        Assert.Equal(HttpStatusCode.OK, omitted.StatusCode);
        var omittedPage = await omitted.Content.ReadFromJsonAsync<ApiPagedResponse<ForumPostDto>>();
        Assert.NotNull(omittedPage);
        Assert.Equal(1, omittedPage!.Page);
        Assert.Equal(ForumRoutes.PostsPageSize, omittedPage.PageSize);
        Assert.Equal(ForumRoutes.PostsPageSize, omittedPage.Items.Count);
        Assert.Equal(2, omittedPage.TotalPages);
        Assert.DoesNotContain(
            omittedPage.Items,
            item => item.Body.Contains("Archive reply 1125", StringComparison.Ordinal));

        using var clamped = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/topics/1002/posts?page=0&pageSize=1000");
        Assert.Equal(HttpStatusCode.OK, clamped.StatusCode);
        var clampedPage = await clamped.Content.ReadFromJsonAsync<ApiPagedResponse<ForumPostDto>>();
        Assert.NotNull(clampedPage);
        Assert.Equal(1, clampedPage!.Page);
        Assert.Equal(ForumRoutes.PostsPageSize, clampedPage.PageSize);
        Assert.Equal(ForumRoutes.PostsPageSize, clampedPage.Items.Count);
    }

    [Fact]
    public async Task Topic_posts_return_empty_page_for_thread_with_no_posts()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/1003/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumPostDto>>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Items);
        Assert.Equal(0, payload.TotalCount);
        Assert.Equal(0, payload.TotalPages);
    }

    [Fact]
    public async Task Topic_posts_return_problem_details_for_missing_thread()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/topics/9999/posts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
