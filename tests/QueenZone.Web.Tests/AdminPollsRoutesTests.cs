using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class AdminPollsRoutesTests
{
    [Fact]
    public async Task AnonymousUserCannotAccessAdminPolls()
    {
        using var isolated = IsolatedHomePolls();
        using var client = isolated.CreateAnonymousClient(allowAutoRedirect: false);

        var response = await client.GetAsync("/admin/polls");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_publish_close_hide_and_delete_a_draft()
    {
        using var isolated = IsolatedHomePolls();
        using var client = isolated.CreateAdminClient();

        var list = await client.GetAsync("/admin/polls");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadAsStringAsync();
        Assert.Contains("Home polls", listBody, StringComparison.Ordinal);
        Assert.Contains("/admin/polls/new", listBody, StringComparison.Ordinal);

        var createdOk = await PostCreateAsync(client, "Admin poll?", "One", "Two");
        Assert.Equal(HttpStatusCode.Redirect, createdOk.StatusCode);

        using var scope = isolated.Services.CreateScope();
        var polls = scope.ServiceProvider.GetRequiredService<IHomePollRepository>();
        var all = await polls.GetAllAsync();
        Assert.Single(all);
        var pollId = all[0].Id;
        Assert.False(all[0].IsCurrent);

        var published = await PostActionAsync(client, "Publish", pollId);
        Assert.Equal(HttpStatusCode.Redirect, published.StatusCode);
        Assert.Equal(pollId, (await polls.GetCurrentAsync(null))!.PollId);

        var closed = await PostActionAsync(client, "Close", pollId);
        Assert.Equal(HttpStatusCode.Redirect, closed.StatusCode);
        Assert.True((await polls.GetCurrentAsync(null))!.IsClosed);

        var hidden = await PostActionAsync(client, "Hide", pollId);
        Assert.Equal(HttpStatusCode.Redirect, hidden.StatusCode);
        Assert.Null(await polls.GetCurrentAsync(null));

        var deleted = await PostActionAsync(client, "Delete", pollId);
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Empty(await polls.GetAllAsync());
    }

    [Fact]
    public async Task Admin_cannot_edit_or_delete_after_the_first_vote()
    {
        using var isolated = IsolatedHomePolls();
        using var client = isolated.CreateAdminClient();
        await PostCreateAsync(client, "Locked?", "Yes", "No");

        using var scope = isolated.Services.CreateScope();
        var polls = scope.ServiceProvider.GetRequiredService<IHomePollRepository>();
        var pollId = (await polls.GetAllAsync())[0].Id;
        await polls.PublishAsync(pollId);
        var current = await polls.GetCurrentAsync(null);
        await polls.CastVoteAsync(current!.Options[0].OptionId, Guid.NewGuid());

        var edit = await client.GetAsync($"/admin/polls/{pollId}/edit");
        var editBody = await edit.Content.ReadAsStringAsync();
        Assert.Contains("locked", editBody, StringComparison.OrdinalIgnoreCase);

        var save = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            $"/admin/polls/{pollId}/edit",
            $"/admin/polls/{pollId}",
            new Dictionary<string, string>
            {
                ["question"] = "Changed?",
                ["optionTexts"] = "X",
            });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var savedBody = await save.Content.ReadAsStringAsync();
        Assert.Contains("cannot be changed", savedBody, StringComparison.OrdinalIgnoreCase);

        var deleted = await PostActionAsync(client, "Delete", pollId);
        var afterDelete = await client.GetStringAsync("/admin/polls");
        Assert.Contains("cannot be deleted", afterDelete, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(await polls.GetByIdAsync(pollId));
        Assert.Equal("Locked?", (await polls.GetByIdAsync(pollId))!.Question);
    }

    [Fact]
    public async Task AuthorizedAdminGetsNotFoundForMissingPoll()
    {
        using var isolated = IsolatedHomePolls();
        using var client = isolated.CreateAdminClient();

        var response = await client.GetAsync($"/admin/polls/{Guid.NewGuid()}/edit");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static QueenZoneWebApplicationFactory IsolatedHomePolls()
    {
        var store = new SharedHomePollStore();
        return QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<SharedHomePollStore>();
            services.RemoveAll<IHomePollRepository>();
            services.AddSingleton(store);
            services.AddSingleton<IHomePollRepository>(_ => new InMemoryHomePollRepository(store));
        });
    }

    private static async Task<HttpResponseMessage> PostCreateAsync(
        HttpClient client,
        string question,
        string option1,
        string option2)
    {
        var formPage = await client.GetStringAsync("/admin/polls/new");
        var token = AdminHttpTestHelpers.ExtractAntiforgeryToken(formPage);
        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("question", question),
            new KeyValuePair<string, string>("optionTexts", option1),
            new KeyValuePair<string, string>("optionTexts", option2),
        ]);
        return await client.PostAsync("/admin/polls", content);
    }

    private static async Task<HttpResponseMessage> PostActionAsync(HttpClient client, string handler, Guid id)
    {
        var listPage = await client.GetStringAsync("/admin/polls");
        var token = AdminHttpTestHelpers.ExtractAntiforgeryToken(listPage);
        return await client.PostAsync(
            $"/admin/polls?handler={handler}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["id"] = id.ToString(),
            }));
    }
}
