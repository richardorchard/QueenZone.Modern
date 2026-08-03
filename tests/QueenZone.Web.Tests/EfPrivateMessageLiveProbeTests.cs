using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe for private messaging IDENTITY SortKey assignment and
/// conversation write-lock serialization. Skipped unless ConnectionStrings__QueenZoneLegacy and
/// RUN_PRIVATE_MESSAGE_PROBE=true are set. Probe scripts refuse targets other than
/// localhost\SQLEXPRESS / queenzone_legacy_sync.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfPrivateMessageLiveProbeTests
{
    [Fact]
    public async Task Identity_sortkey_and_write_lock_probe_when_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        Guid? conversationId = null;

        try
        {
            await using (var setup = CreateContext(connectionString))
            {
                Assert.True(
                    await ColumnExistsAsync(setup, "PrivateConversations", "LastMessageSortKey"),
                    "PrivateConversations.LastMessageSortKey is missing. Apply EF migrations before running this probe.");

                setup.MemberAccounts.AddRange(
                    NewProbeMember(aliceId, $"pm-probe-alice-{uniqueSuffix}@queenzone.local", $"PM Probe Alice {uniqueSuffix}"),
                    NewProbeMember(bobId, $"pm-probe-bob-{uniqueSuffix}@queenzone.local", $"PM Probe Bob {uniqueSuffix}"));
                await setup.SaveChangesAsync();
            }

            async Task<PrivateMessageSendResult> SendAsync(string body, DateTimeOffset sentAt)
            {
                await using var context = CreateContext(connectionString);
                var repo = new EfPrivateMessageRepository(context);
                return await repo.SendNewOrExistingAsync(aliceId, bobId, body, sentAt);
            }

            var firstSendResults = await Task.WhenAll(
                SendAsync($"Probe concurrent A {uniqueSuffix}", DateTimeOffset.UtcNow),
                SendAsync($"Probe concurrent B {uniqueSuffix}", DateTimeOffset.UtcNow.AddMilliseconds(1)));

            Assert.All(firstSendResults, r => Assert.True(r.Succeeded, r.ErrorMessage));
            Assert.Equal(firstSendResults[0].ConversationId, firstSendResults[1].ConversationId);
            conversationId = firstSendResults[0].ConversationId;

            async Task<PrivateMessageSendResult> ReplyAsync(Guid senderId, string body)
            {
                await using var context = CreateContext(connectionString);
                var repo = new EfPrivateMessageRepository(context);
                return await repo.ReplyAsync(conversationId!.Value, senderId, body, DateTimeOffset.UtcNow);
            }

            const int replyCount = 8;
            var replyResults = await Task.WhenAll(
                Enumerable.Range(0, replyCount).Select(i =>
                    ReplyAsync(i % 2 == 0 ? aliceId : bobId, $"Probe reply {i} {uniqueSuffix}")));

            Assert.All(replyResults, r => Assert.True(r.Succeeded, r.ErrorMessage));

            Assert.NotNull(conversationId);
            var probeConversationId = conversationId.Value;

            await using var verify = CreateContext(connectionString);
            var messages = await verify.PrivateMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == probeConversationId)
                .OrderBy(m => m.SortKey)
                .ToListAsync();

            Assert.Equal(replyCount + 2, messages.Count);
            Assert.Equal(messages.Count, messages.Select(m => m.SortKey).Distinct().Count());
            Assert.All(messages, m => Assert.True(m.SortKey > 0));
            for (var i = 1; i < messages.Count; i++)
            {
                Assert.True(messages[i].SortKey > messages[i - 1].SortKey);
            }

            var conversation = await verify.PrivateConversations
                .AsNoTracking()
                .SingleAsync(c => c.Id == probeConversationId);
            Assert.Equal(messages[^1].SortKey, conversation.LastMessageSortKey);
            Assert.Equal(messages[^1].Body, conversation.LastMessagePreview);

            var inbox = await new EfPrivateMessageRepository(verify).GetInboxAsync(aliceId);
            var inboxItem = Assert.Single(inbox.Items, i => i.ConversationId == probeConversationId);
            Assert.Equal(messages[^1].Body, inboxItem.LastMessagePreview);
        }
        finally
        {
            await CleanupProbeRowsAsync(connectionString, aliceId, bobId, conversationId);
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

    private static async Task<bool> ColumnExistsAsync(
        QueenZoneDbContext dbContext,
        string tableName,
        string columnName)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        command.CommandText =
            """
            SELECT CASE WHEN COL_LENGTH(@table, @column) IS NULL THEN 0 ELSE 1 END
            """;
        var table = command.CreateParameter();
        table.ParameterName = "@table";
        table.Value = $"dbo.{tableName}";
        command.Parameters.Add(table);
        var column = command.CreateParameter();
        column.ParameterName = "@column";
        column.Value = columnName;
        command.Parameters.Add(column);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 1;
    }

    private static async Task CleanupProbeRowsAsync(
        string connectionString,
        Guid aliceId,
        Guid bobId,
        Guid? conversationId)
    {
        await using var cleanup = CreateContext(connectionString);
        if (conversationId is Guid id)
        {
            await cleanup.PrivateConversations
                .Where(c => c.Id == id)
                .ExecuteDeleteAsync();
        }

        await cleanup.MemberAccounts
            .Where(m => m.Id == aliceId || m.Id == bobId)
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
            Environment.GetEnvironmentVariable("RUN_PRIVATE_MESSAGE_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
