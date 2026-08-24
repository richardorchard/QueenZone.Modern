using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfLiveActivityQueryServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly QueenZoneDbContext dbContext;

    public EfLiveActivityQueryServiceTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        CreateModernForumPostTable();
    }

    [Fact]
    public async Task GetNewForumRepliesTodayAsync_counts_only_visible_posts_from_today()
    {
        var now = new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        InsertPost(postedAt: new DateTime(2026, 8, 24, 9, 0, 0, DateTimeKind.Utc), isHidden: false);
        InsertPost(postedAt: new DateTime(2026, 8, 24, 23, 59, 0, DateTimeKind.Utc), isHidden: false);
        InsertPost(postedAt: new DateTime(2026, 8, 23, 23, 59, 0, DateTimeKind.Utc), isHidden: false);
        InsertPost(postedAt: new DateTime(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc), isHidden: true);
        InsertPost(postedAt: null, isHidden: false);

        var service = new EfLiveActivityQueryService(dbContext, timeProvider);
        var count = await service.GetNewForumRepliesTodayAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetNewForumRepliesTodayAsync_returns_zero_when_no_posts_exist()
    {
        var service = new EfLiveActivityQueryService(dbContext, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var count = await service.GetNewForumRepliesTodayAsync();

        Assert.Equal(0, count);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    private int nextLegacyId = 1;

    private void InsertPost(DateTime? postedAt, bool isHidden)
    {
        var id = nextLegacyId++;
        dbContext.Database.ExecuteSqlInterpolated($"""
            INSERT INTO ModernForumPost
                (LegacyPostId, LegacyThreadTopicId, ThreadId, LegacyForumId, AuthorDisplayName,
                 BodyHtml, PostedAt, EditCount, LegacyDiscography, AttachCount, IsHidden,
                 ImportedAt, UpdatedAt)
            VALUES
                ({id}, {id}, {id}, 1, 'Tester',
                 '<p>Hello</p>', {postedAt}, 0, 0, 0, {(isHidden ? 1 : 0)},
                 '2026-08-01T00:00:00', '2026-08-01T00:00:00')
            """);
    }

    private void CreateModernForumPostTable()
    {
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE ModernForumPost
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LegacyPostId INTEGER NOT NULL UNIQUE,
                LegacyThreadTopicId INTEGER NOT NULL,
                ThreadId INTEGER NOT NULL,
                LegacyForumId INTEGER NOT NULL,
                AuthorLegacyUserId INTEGER NULL,
                AuthorDisplayName TEXT NOT NULL,
                AuthorPostCount INTEGER NULL,
                AuthorJoinedAt TEXT NULL,
                BodyHtml TEXT NOT NULL,
                SignatureHtml TEXT NULL,
                PostedAt TEXT NULL,
                LegacyDiscography INTEGER NOT NULL,
                AuthorUserValidated INTEGER NULL,
                Attachment TEXT NULL,
                FileSize TEXT NULL,
                AttachCount INTEGER NOT NULL,
                AuthorMemberId TEXT NULL,
                EditedAt TEXT NULL,
                EditCount INTEGER NOT NULL DEFAULT 0,
                IsHidden INTEGER NOT NULL DEFAULT 0,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
