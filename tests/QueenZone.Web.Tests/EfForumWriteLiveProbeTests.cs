using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe for modern forum thread/post writes (sequences + stats).
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfForumWriteLiveProbeTests
{
    [Fact]
    public async Task Create_thread_and_reply_on_mirror_when_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var memberId = Guid.NewGuid();
        var marker = $"forum-write-probe-{uniqueSuffix}";
        int? topicId = null;

        try
        {
            await using (var setup = CreateContext(connectionString))
            {
                var category = await setup.ModernForumCategories
                    .AsNoTracking()
                    .Where(c => !c.IsSynthetic)
                    .OrderBy(c => c.LegacyForumId)
                    .FirstOrDefaultAsync();
                Assert.NotNull(category);
                Assert.True(
                    category.LegacyForumId > 0,
                    "Mirror has no non-synthetic modern forum category. Import/project forum categories before this probe.");

                setup.MemberAccounts.Add(NewProbeMember(
                    memberId,
                    $"{marker}@queenzone.local",
                    $"Forum Write Probe {uniqueSuffix}"));
                await setup.SaveChangesAsync();

                var repo = new EfForumWriteRepository(setup);
                var created = await repo.CreateThreadAsync(new NewForumThread(
                    category.LegacyForumId,
                    memberId,
                    $"Forum Write Probe {uniqueSuffix}",
                    $"{marker} subject",
                    $"<p>{marker} starter body</p>",
                    DateTimeOffset.UtcNow));
                topicId = created.TopicId;

                var replyId = await repo.CreatePostAsync(new NewForumPost(
                    created.TopicId,
                    memberId,
                    $"Forum Write Probe {uniqueSuffix}",
                    $"<p>{marker} reply body</p>",
                    DateTimeOffset.UtcNow));
                Assert.True(replyId > created.StarterPostId);

                var thread = await repo.GetThreadAsync(created.TopicId);
                Assert.NotNull(thread);
                Assert.Equal(2, thread.PostCount);
                Assert.Contains(marker, thread.Subject, StringComparison.Ordinal);
            }
        }
        finally
        {
            await CleanupAsync(connectionString, memberId, topicId, marker);
        }
    }

    private static QueenZoneDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.CommandTimeout(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                        maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                        errorNumbersToAdd: null);
                })
            .Options;
        return new QueenZoneDbContext(options);
    }

    private static MemberAccount NewProbeMember(Guid id, string email, string displayName) =>
        new()
        {
            Id = id,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        };

    private static async Task CleanupAsync(
        string connectionString,
        Guid memberId,
        int? topicId,
        string marker)
    {
        await using var cleanup = CreateContext(connectionString);
        if (topicId is int legacyTopicId)
        {
            var thread = await cleanup.ModernForumThreads
                .SingleOrDefaultAsync(t => t.LegacyTopicId == legacyTopicId);
            if (thread is not null)
            {
                await cleanup.Database.ExecuteSqlRawAsync(
                    """
                    IF OBJECT_ID(N'dbo.ModernForumThreadReadStats', N'U') IS NOT NULL
                    BEGIN
                        DELETE FROM dbo.ModernForumThreadReadStats WHERE LegacyTopicId = {0};
                    END
                    """,
                    legacyTopicId);

                await cleanup.ModernForumPosts
                    .Where(p => p.ThreadId == thread.Id)
                    .ExecuteDeleteAsync();
                await cleanup.ModernForumThreads
                    .Where(t => t.Id == thread.Id)
                    .ExecuteDeleteAsync();
            }
        }

        // Category LegacyPostCount / read-stat counters may retain tiny disposable-mirror drift.
        await cleanup.MemberAccounts
            .Where(m => m.Id == memberId || m.Email.Contains(marker))
            .ExecuteDeleteAsync();
    }

    private static bool IsProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_FORUM_WRITE_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
