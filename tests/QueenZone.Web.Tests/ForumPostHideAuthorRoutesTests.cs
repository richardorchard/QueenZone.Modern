using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumPostHideAuthorRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ForumPostHideAuthorRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task HideAuthorGet_RedirectsUnauthenticatedUsers()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await CreateOwnedPostAsync(Guid.NewGuid(), "Spam opener");

        var response = await client.GetAsync($"/forum/hide-author?postId={postId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task HideAuthorGet_Returns403ForNonAdmins()
    {
        var postId = await CreateOwnedPostAsync(Guid.NewGuid(), "Spam opener");
        var client = CreateMemberClient(Guid.NewGuid());

        var response = await client.GetAsync($"/forum/hide-author?postId={postId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("You do not have permission to hide forum content.", html);
    }

    [Fact]
    public async Task AdminCanHideAllByAuthorFromThread_AndOtherPostsStay()
    {
        var spammerId = Guid.NewGuid();
        var innocentId = Guid.NewGuid();
        var spamPostId = await CreateOwnedPostAsync(spammerId, "Spam opener", "Spammer");
        using var scope = factory.Services.CreateScope();
        var write = scope.ServiceProvider.GetRequiredService<IForumWriteRepository>();
        var spam = await write.GetPostAsync(spamPostId);
        Assert.NotNull(spam);
        await write.CreatePostAsync(new NewForumPost(
            spam.TopicId, innocentId, "Innocent", "<p>Keep this reply</p>", DateTimeOffset.UtcNow));

        var admin = CreateMemberClient(Guid.NewGuid(), email: "admin@test.local");
        var topicHtml = await admin.GetStringAsync(
            ForumRoutes.GetTopicCanonicalPath(spam.TopicId, spam.TopicSubject));
        Assert.Contains($"href=\"/forum/hide-author?postId={spamPostId}\"", topicHtml);

        var confirm = await admin.GetStringAsync($"/forum/hide-author?postId={spamPostId}");
        Assert.Contains("Hide all posts and threads started by", confirm);
        Assert.Contains("Other people", confirm);
        Assert.Contains("Spammer", confirm);

        var hide = await admin.PostAsync(
            $"/forum/hide-author?postId={spamPostId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(confirm),
                ["postId"] = spamPostId.ToString(),
            }));
        Assert.Equal(HttpStatusCode.Redirect, hide.StatusCode);

        var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var after = await guest.GetAsync(ForumRoutes.GetTopicCanonicalPath(spam.TopicId, spam.TopicSubject));
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);

        var inMemory = write as InMemoryForumWriteRepository
            ?? throw new InvalidOperationException("Expected in-memory forum write repository in Testing.");
        var remaining = inMemory.GetPostsForTopic(spam.TopicId);
        Assert.Single(remaining);
        Assert.Equal(innocentId, remaining[0].MemberId);
    }

    [Fact]
    public async Task TopicPage_DoesNotShowHideAuthor_ForOrdinaryMembers()
    {
        var postId = await CreateOwnedPostAsync(Guid.NewGuid(), "Ordinary post");
        using var scope = factory.Services.CreateScope();
        var post = await scope.ServiceProvider.GetRequiredService<IForumWriteRepository>().GetPostAsync(postId);
        var member = CreateMemberClient(Guid.NewGuid());

        var html = await member.GetStringAsync(
            ForumRoutes.GetTopicCanonicalPath(post!.TopicId, post.TopicSubject));

        Assert.DoesNotContain("/forum/hide-author", html);
    }

    [Fact]
    public async Task AdminHideReplyAuthor_LeavesOtherPeoplesPostsVisible()
    {
        var openerId = Guid.NewGuid();
        var spammerId = Guid.NewGuid();
        var openerPostId = await CreateOwnedPostAsync(openerId, "Real opener", "Opener");
        using var scope = factory.Services.CreateScope();
        var write = scope.ServiceProvider.GetRequiredService<IForumWriteRepository>();
        var opener = await write.GetPostAsync(openerPostId);
        var spamReplyId = await write.CreatePostAsync(new NewForumPost(
            opener!.TopicId, spammerId, "ReplySpammer", "<p>Spam reply</p>", DateTimeOffset.UtcNow));

        var admin = CreateMemberClient(Guid.NewGuid(), email: "admin@test.local");
        var confirm = await admin.GetStringAsync($"/forum/hide-author?postId={spamReplyId}");
        var hide = await admin.PostAsync(
            $"/forum/hide-author?postId={spamReplyId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(confirm),
                ["postId"] = spamReplyId.ToString(),
            }));
        Assert.Equal(HttpStatusCode.Redirect, hide.StatusCode);

        var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var topic = await guest.GetStringAsync(
            ForumRoutes.GetTopicCanonicalPath(opener.TopicId, opener.TopicSubject));
        Assert.Contains("Real opener", topic);
        Assert.DoesNotContain("Spam reply", topic);
    }

    private async Task<int> CreateOwnedPostAsync(Guid memberId, string body, string displayName = "Forum Fan")
    {
        var client = CreateMemberClient(memberId, displayName);
        var form = await client.GetStringAsync("/forum/c/the-music/new-thread");
        var token = ExtractAntiforgeryToken(form);
        var response = await client.PostAsync("/forum/c/the-music/new-thread", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Subject"] = $"Hide author topic {memberId:N}",
            ["Body"] = body,
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var created = scope.ServiceProvider.GetRequiredService<IForumWriteRepository>() as InMemoryForumWriteRepository
            ?? throw new InvalidOperationException("Expected in-memory forum write repository in Testing.");
        var topicId = int.Parse(Regex.Match(response.Headers.Location!.OriginalString, @"/forum/topic/(\d+)/").Groups[1].Value);
        return created.GetPostsForTopic(topicId).Single().PostId;
    }

    private HttpClient CreateMemberClient(Guid memberId, string displayName = "Forum Fan", string? email = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, memberId.ToString());
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, displayName);
        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Add(TestMemberAuthHandler.EmailHeader, email);
        }

        return client;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var input = Regex.Match(
            html,
            """<input[^>]*name="__RequestVerificationToken"[^>]*>""",
            RegexOptions.IgnoreCase);
        Assert.True(input.Success, "Antiforgery token input was not found in the form.");

        var value = Regex.Match(input.Value, "value=\"(?<token>[^\"]+)\"", RegexOptions.IgnoreCase);
        Assert.True(value.Success, "Antiforgery token value was not found in the form.");
        return value.Groups["token"].Value;
    }
}
