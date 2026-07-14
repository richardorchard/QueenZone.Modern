using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumPollFormAndEndpointUnitTests
{
    [Fact]
    public void ForumPollForm_Disabled_ReturnsNullWithoutErrors()
    {
        var form = new ForumPollForm { Enabled = false };
        var errors = new List<string>();
        Assert.Null(form.ToNewPoll(Guid.NewGuid(), errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void ForumPollForm_RejectsPastCloseDate()
    {
        var form = new ForumPollForm
        {
            Enabled = true,
            Question = "Q",
            Options = ["A", "B"],
            ClosesAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        var errors = new List<string>();
        Assert.Null(form.ToNewPoll(Guid.NewGuid(), errors));
        Assert.Contains(errors, e => e.Contains("future", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ForumPollForm_RejectsTooManyOptions()
    {
        var form = new ForumPollForm
        {
            Enabled = true,
            Question = "Q",
            Options = Enumerable.Range(1, 11).Select(i => $"O{i}").ToList(),
        };
        var errors = new List<string>();
        Assert.Null(form.ToNewPoll(Guid.NewGuid(), errors));
        Assert.Contains(errors, e => e.Contains("at most", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsAdmin_MatchesAllowedEmail()
    {
        var options = new AdminOptions { AllowedEmails = ["admin@example.com"] };
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "admin@example.com"),
        ], "test"));

        Assert.True(ForumPollEndpoints.IsAdmin(user, options));
        Assert.False(ForumPollEndpoints.IsAdmin(new ClaimsPrincipal(new ClaimsIdentity()), options));
    }

    [Fact]
    public async Task VoteAsync_ReturnsUnauthorized_WhenAnonymous()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST";
        http.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddAntiforgery()
            .BuildServiceProvider();

        var result = await ForumPollEndpoints.VoteAsync(
            Guid.NewGuid(),
            http,
            new InMemoryForumPollRepository(),
            http.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>(),
            CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task CloseAsync_ReturnsUnauthorized_WhenAnonymous()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST";
        http.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddAntiforgery()
            .BuildServiceProvider();

        var result = await ForumPollEndpoints.CloseAsync(
            Guid.NewGuid(),
            http,
            new InMemoryForumPollRepository(),
            http.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>(),
            new AdminOptions(),
            CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void BuildResults_ComputesClosedAndPercentages()
    {
        var pollId = Guid.NewGuid();
        var optionA = Guid.NewGuid();
        var optionB = Guid.NewGuid();
        var member = Guid.NewGuid();
        var poll = new QueenZone.Data.Entities.ForumPollEntity
        {
            Id = pollId,
            LegacyTopicId = 1,
            Question = "Q",
            IsMultiChoice = false,
            CreatedByMemberId = member,
            CreatedAt = DateTimeOffset.UtcNow,
            ClosesAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Options =
            [
                new() { Id = optionA, PollId = pollId, OptionText = "A", DisplayOrder = 0 },
                new() { Id = optionB, PollId = pollId, OptionText = "B", DisplayOrder = 1 },
            ],
        };

        var results = EfForumPollRepository.BuildResults(
            poll,
            [(optionA, member), (optionA, Guid.NewGuid()), (optionB, Guid.NewGuid())],
            member,
            viewerIsAdmin: false,
            DateTimeOffset.UtcNow);

        Assert.True(results.IsClosed);
        Assert.False(results.CanViewerVote);
        Assert.Equal(66.7, results.Options[0].Percentage);
        Assert.True(results.Options[0].SelectedByViewer);
    }
}
