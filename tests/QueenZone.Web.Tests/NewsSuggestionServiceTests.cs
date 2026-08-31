using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class NewsSuggestionServiceTests
{
    [Fact]
    public async Task SubmitAsync_ReturnsDuplicateActive_WhenActiveUrlAlreadySuggested()
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

        var duplicate = Assert.IsType<SubmitOutcome.DuplicateActive>(result);
        Assert.Equal(NewsSuggestionService.DuplicateActiveMessage, duplicate.Message);
        Assert.Equal(1, await repository.CountBySubmitterSinceAsync(memberId, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task SubmitAsync_ReturnsDuplicateActive_WhenRepositoryThrowsTypedRaceException()
    {
        var service = new NewsSuggestionService(
            new DuplicateThrowingNewsSuggestionRepository(),
            Options.Create(new NewsSuggestionOptions()));

        var result = await service.SubmitAsync(
            Guid.NewGuid(),
            "https://example.com/race-story",
            "Race",
            null,
            CancellationToken.None);

        var duplicate = Assert.IsType<SubmitOutcome.DuplicateActive>(result);
        Assert.Equal(NewsSuggestionService.DuplicateActiveMessage, duplicate.Message);
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
            Assert.IsType<SubmitOutcome.Accepted>(result);
        }

        var blocked = await service.SubmitAsync(
            memberId,
            "https://example.com/story-extra",
            "Extra",
            null,
            CancellationToken.None);

        var limit = Assert.IsType<SubmitOutcome.DailyLimit>(blocked);
        Assert.Contains("5 news stories per day", limit.Message, StringComparison.Ordinal);
        Assert.Equal(5, await repository.CountBySubmitterSinceAsync(memberId, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task SubmitAsync_RejectsEmptyMemberId()
    {
        var service = new NewsSuggestionService(
            new InMemoryNewsSuggestionRepository(),
            Options.Create(new NewsSuggestionOptions()));

        var result = await service.SubmitAsync(Guid.Empty, "https://example.com/story", null, null);

        var signIn = Assert.IsType<SubmitOutcome.SignInRequired>(result);
        Assert.Contains("Sign in is required", signIn.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "URL is required.")]
    [InlineData("", "URL is required.")]
    [InlineData("http://example.com/story", "URL must be a well-formed https:// link.")]
    [InlineData("not-a-url", "URL must be a well-formed https:// link.")]
    [InlineData("https://localhost/story", "URL must be a public https:// link.")]
    [InlineData("https://127.0.0.1/story", "URL must be a public https:// link.")]
    [InlineData("https://user:pass@example.com/story", "URL must not include credentials.")]
    public async Task SubmitAsync_RejectsInvalidUrls(string? url, string expectedMessage)
    {
        var service = new NewsSuggestionService(
            new InMemoryNewsSuggestionRepository(),
            Options.Create(new NewsSuggestionOptions()));

        var result = await service.SubmitAsync(Guid.NewGuid(), url!, null, null);

        var invalid = Assert.IsType<SubmitOutcome.InvalidField>(result);
        Assert.Equal(expectedMessage, invalid.Message);
    }

    [Fact]
    public async Task SubmitAsync_RejectsOverlongTitleAndNotes()
    {
        var service = new NewsSuggestionService(
            new InMemoryNewsSuggestionRepository(),
            Options.Create(new NewsSuggestionOptions()));

        var titleResult = await service.SubmitAsync(
            Guid.NewGuid(),
            "https://example.com/story",
            new string('t', 301),
            null);
        var titleInvalid = Assert.IsType<SubmitOutcome.InvalidField>(titleResult);
        Assert.Contains("300 characters", titleInvalid.Message, StringComparison.Ordinal);

        var notesResult = await service.SubmitAsync(
            Guid.NewGuid(),
            "https://example.com/story",
            null,
            new string('n', 1001));
        var notesInvalid = Assert.IsType<SubmitOutcome.InvalidField>(notesResult);
        Assert.Contains("1000 characters", notesInvalid.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PromoteToAdminDraftAsync_CreatesDraftUpdatesSuggestionAndAudits()
    {
        var newsStore = new SharedNewsStore();
        var admin = new InMemoryAdminNewsRepository(newsStore);
        var suggestions = new InMemoryNewsSuggestionRepository();
        var created = await suggestions.CreateAsync(
            new NewsSuggestion(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "https://example.com/promote-story",
                NewsCandidateDedupe.ComputeUrlHash("https://example.com/promote-story"),
                "Promote headline",
                "Editor notes",
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
            suggestions,
            Options.Create(new NewsSuggestionOptions()),
            admin,
            new InMemoryNewsAuditRepository(newsStore));
        var adminDraft = NewsSuggestionPromoteDraft.Build(created);

        var newsId = await service.PromoteToAdminDraftAsync(
            created,
            adminDraft,
            "editor@test.local",
            "Looks good");

        var article = await admin.GetByIdAsync(newsId);
        Assert.NotNull(article);
        Assert.False(article!.IsPublished);
        var updated = await suggestions.GetByIdAsync(created.Id);
        Assert.Equal(NewsSuggestionStatus.Promoted, updated!.Status);
        Assert.Equal(newsId, updated.PromotedNewsId);
        var audit = Assert.Single(await new InMemoryNewsAuditRepository(newsStore).GetByNewsIdAsync(newsId));
        Assert.Equal("promote-from-suggestion", audit.Action);
        Assert.Contains(created.Id.ToString(), audit.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PromoteToAdminDraftAsync_Throws_WhenSuggestionPromoteReturnsNull()
    {
        var newsStore = new SharedNewsStore();
        var inner = new InMemoryNewsSuggestionRepository();
        var created = await inner.CreateAsync(
            new NewsSuggestion(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "https://example.com/missing-promote",
                NewsCandidateDedupe.ComputeUrlHash("https://example.com/missing-promote"),
                "Headline",
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
        var suggestions = new ConfigurableNewsSuggestionRepository(inner)
        {
            PromoteHandler = (_, _, _, _, _) => Task.FromResult<NewsSuggestion?>(null),
        };
        var service = new NewsSuggestionService(
            suggestions,
            Options.Create(new NewsSuggestionOptions()),
            new InMemoryAdminNewsRepository(newsStore),
            new InMemoryNewsAuditRepository(newsStore));

        var ex = await Assert.ThrowsAsync<AdminNewsPromotionException>(() =>
            service.PromoteToAdminDraftAsync(
                created,
                NewsSuggestionPromoteDraft.Build(created),
                "editor@test.local",
                null));

        Assert.Equal("Promotion failed while updating the suggestion.", ex.Message);
    }

    [Fact]
    public void ValidateUrl_RejectsOverlongUrl()
    {
        var error = NewsSuggestionService.ValidateUrl("https://example.com/" + new string('a', 2000));
        Assert.Contains("2000 characters", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateUrl_RejectsHttpWithoutUpgrade()
    {
        Assert.Equal(
            "URL must be a well-formed https:// link.",
            NewsSuggestionService.ValidateUrl("http://example.com/story"));
    }

    [Fact]
    public void IsActiveUrlHashUniqueViolation_RequiresSqlUniqueAndIndexName()
    {
        var matching = new DbUpdateException(
            "conflict",
            CreateSqlException(2601, $"duplicate key on {EfNewsSuggestionRepository.ActiveUrlHashIndexName}"));
        Assert.True(EfNewsSuggestionRepository.IsActiveUrlHashUniqueViolation(matching));

        var otherIndex = new DbUpdateException(
            "conflict",
            CreateSqlException(2627, "duplicate key on IX_SomethingElse"));
        Assert.False(EfNewsSuggestionRepository.IsActiveUrlHashUniqueViolation(otherIndex));

        var namedButWrongNumber = new DbUpdateException(
            $"conflict on {EfNewsSuggestionRepository.ActiveUrlHashIndexName}",
            CreateSqlException(208, "Invalid object name."));
        Assert.False(EfNewsSuggestionRepository.IsActiveUrlHashUniqueViolation(namedButWrongNumber));

        var generic = new DbUpdateException("save failed", new InvalidOperationException("nope"));
        Assert.False(EfNewsSuggestionRepository.IsActiveUrlHashUniqueViolation(generic));
    }

    private static SqlException CreateSqlException(int number, string message)
    {
        var sqlClient = typeof(SqlException).Assembly;
        var errorCollectionType = sqlClient.GetType("Microsoft.Data.SqlClient.SqlErrorCollection")
            ?? throw new InvalidOperationException("SqlErrorCollection type not found.");
        var errorType = sqlClient.GetType("Microsoft.Data.SqlClient.SqlError")
            ?? throw new InvalidOperationException("SqlError type not found.");

        var collection = Activator.CreateInstance(errorCollectionType, nonPublic: true)
            ?? throw new InvalidOperationException("Unable to create SqlErrorCollection.");

        var errorCtor = errorType.GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var errorArgs = errorCtor.GetParameters().Select(p =>
        {
            if (p.Name is "infoNumber" or "number")
            {
                return number;
            }

            if (p.ParameterType == typeof(int))
            {
                return 0;
            }

            if (p.ParameterType == typeof(byte))
            {
                return (byte)16;
            }

            if (p.ParameterType == typeof(string))
            {
                return p.Name is "errorMessage" or "message" ? message : "server";
            }

            if (p.ParameterType == typeof(uint))
            {
                return 0u;
            }

            if (typeof(Exception).IsAssignableFrom(p.ParameterType))
            {
                return null!;
            }

            return p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)! : null!;
        }).ToArray();
        var error = errorCtor.Invoke(errorArgs);

        errorCollectionType
            .GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(collection, [error]);

        var createException = typeof(SqlException)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == "CreateException")
            .OrderBy(m => m.GetParameters().Length)
            .First();
        var createArgs = createException.GetParameters().Select(p =>
        {
            if (p.ParameterType == errorCollectionType)
            {
                return collection;
            }

            if (p.ParameterType == typeof(string))
            {
                return "12.0.0";
            }

            if (p.ParameterType == typeof(Guid))
            {
                return Guid.Empty;
            }

            return null!;
        }).ToArray();

        return (SqlException)createException.Invoke(null, createArgs)!;
    }

    private sealed class DuplicateThrowingNewsSuggestionRepository : INewsSuggestionRepository
    {
        public Task<NewsSuggestion> CreateAsync(
            NewsSuggestion suggestion,
            CancellationToken cancellationToken = default) =>
            throw new DuplicateActiveNewsSuggestionException();

        public Task<bool> HasActiveDuplicateAsync(
            string urlHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> CountBySubmitterSinceAsync(
            Guid submitterMemberId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<NewsSuggestionListItem>> GetPendingAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SubmissionListPage<NewsSuggestion>> GetBySubmitterAsync(
            Guid submitterMemberId,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsSuggestion?> UpdateStatusAsync(
            Guid id,
            string status,
            string? reviewerEmail,
            string? notes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsSuggestion?> PromoteAsync(
            Guid id,
            int promotedNewsId,
            string reviewerEmail,
            string? reviewNotes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsSuggestion?> MarkDuplicateAsync(
            Guid id,
            int duplicateCandidateId,
            string reviewerEmail,
            string? reviewNotes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SubmissionTypeCounts> GetDashboardCountsAsync(
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SubmissionContributor>> GetTopContributorsThisMonthAsync(
            DateTimeOffset monthStart,
            int maxCount,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
