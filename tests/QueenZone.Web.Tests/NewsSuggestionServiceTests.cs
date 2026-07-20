using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsSuggestionServiceTests
{
    [Fact]
    public async Task SubmitAsync_ReturnsDuplicateMessage_WhenActiveUrlAlreadySuggested()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        var memberId = Guid.NewGuid();
        var url = "https://example.com/queen-story?utm_source=test";
        var urlHash = NewsCandidateDedupe.ComputeUrlHash(url);

        await repository.CreateAsync(
            new NewsSuggestion(
                Guid.NewGuid(),
                memberId,
                NewsCandidateDedupe.NormalizeCanonicalUrl(url),
                urlHash,
                "Existing",
                null,
                NewsSuggestionStatus.Pending,
                DateTimeOffset.UtcNow,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var service = new NewsSuggestionService(
            repository,
            Options.Create(new NewsSuggestionOptions()));

        var result = await service.SubmitAsync(
            Guid.NewGuid(),
            "https://example.com/queen-story/",
            "Another headline",
            "Notes",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.IsDuplicateActive);
        Assert.Equal(NewsSuggestionService.DuplicateActiveMessage, result.Error);
        Assert.Equal(1, await repository.CountBySubmitterSinceAsync(memberId, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task SubmitAsync_EnforcesDailyRateLimit_PerMember()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        var memberId = Guid.NewGuid();
        var service = new NewsSuggestionService(
            repository,
            Options.Create(new NewsSuggestionOptions { MaxSubmissionsPerMemberPerDay = 5 }));

        for (var i = 0; i < 5; i++)
        {
            var result = await service.SubmitAsync(
                memberId,
                $"https://example.com/story-{i}",
                $"Story {i}",
                null,
                CancellationToken.None);
            Assert.True(result.Succeeded, result.Error);
        }

        var blocked = await service.SubmitAsync(
            memberId,
            "https://example.com/story-extra",
            "Extra",
            null,
            CancellationToken.None);

        Assert.False(blocked.Succeeded);
        Assert.Contains("5 news stories per day", blocked.Error, StringComparison.Ordinal);
        Assert.Equal(5, await repository.CountBySubmitterSinceAsync(memberId, DateTimeOffset.UtcNow.AddDays(-1)));
    }
}
