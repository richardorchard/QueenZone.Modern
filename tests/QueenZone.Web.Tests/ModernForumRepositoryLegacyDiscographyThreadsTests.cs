using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class ModernForumRepositoryLegacyDiscographyThreadsTests : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly QueenZoneDbContext dbContext;
    private readonly ModernForumRepository repository;

    public ModernForumRepositoryLegacyDiscographyThreadsTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        CreateModernForumTables();
        repository = new ModernForumRepository(dbContext);
    }

    [Fact]
    public async Task GetLegacyDiscographyThreadsAsync_ReturnsOnlyFlaggedThreads_OrderedByTitle()
    {
        var rarities = await SeedCategoryAsync(legacyForumId: 3, name: "Recordings & Rarities", isSynthetic: false);
        await SeedCategoryAsync(legacyForumId: 99, name: "Synthetic", isSynthetic: true);

        await SeedThreadAsync(rarities.Id, 3, legacyTopicId: 30, title: "Zebra sessions", discography: 1, validated: true);
        await SeedThreadAsync(rarities.Id, 3, legacyTopicId: 31, title: "Alpha sessions", discography: 1, validated: true);
        await SeedThreadAsync(rarities.Id, 3, legacyTopicId: 32, title: "Not flagged", discography: 0, validated: true);
        await SeedThreadAsync(rarities.Id, 3, legacyTopicId: 33, title: "Unvalidated flagged", discography: 1, validated: false);
        await SeedThreadAsync(rarities.Id, 3, legacyTopicId: 34, title: "Hidden flagged", discography: 1, validated: true, isHidden: true);
        await SeedThreadAsync(rarities.Id, 3, legacyTopicId: 35, title: "Not a starter flagged", discography: 1, validated: true, isStarter: false);

        var threads = await repository.GetLegacyDiscographyThreadsAsync();

        Assert.Equal(2, threads.Count);
        Assert.Equal(31, threads[0].TopicId);
        Assert.Equal("Alpha sessions", threads[0].Title);
        Assert.Equal(30, threads[1].TopicId);
        Assert.Equal("Zebra sessions", threads[1].Title);
    }

    private async Task<ModernForumCategoryEntity> SeedCategoryAsync(int legacyForumId, string name, bool isSynthetic)
    {
        var category = new ModernForumCategoryEntity
        {
            LegacyForumId = legacyForumId,
            Name = name,
            Description = name,
            SortOrder = legacyForumId,
            LegacyPostCount = 0,
            IsSynthetic = isSynthetic,
            ImportedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        dbContext.ModernForumCategories.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    private async Task SeedThreadAsync(
        int categoryId,
        int legacyForumId,
        int legacyTopicId,
        string title,
        byte discography,
        bool validated,
        bool isStarter = true,
        bool isHidden = false)
    {
        dbContext.ModernForumThreads.Add(new ModernForumThreadEntity
        {
            LegacyTopicId = legacyTopicId,
            LegacyForumId = legacyForumId,
            CategoryId = categoryId,
            Title = title,
            StartedByDisplayName = "John S Stuart",
            LastActivityAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(legacyTopicId),
            ReplyCount = legacyTopicId,
            IsSticky = false,
            IsLegacyTopicStarter = isStarter,
            LegacyDiscography = discography,
            StartedByUserValidated = validated,
            IsHidden = isHidden,
            StarterAttachCount = 0,
            ImportedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
    }

    private void CreateModernForumTables()
    {
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE ModernForumCategory
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LegacyForumId INTEGER NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                SortOrder INTEGER NOT NULL,
                LegacyPostCount INTEGER NOT NULL,
                LastActivityAt TEXT NULL,
                IsSynthetic INTEGER NOT NULL DEFAULT 0,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE ModernForumThread
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LegacyTopicId INTEGER NOT NULL UNIQUE,
                LegacyForumId INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                StartedByLegacyUserId INTEGER NULL,
                StartedByDisplayName TEXT NOT NULL,
                StartedAt TEXT NULL,
                LastActivityAt TEXT NULL,
                ReplyCount INTEGER NOT NULL,
                IsSticky INTEGER NOT NULL,
                IsLegacyTopicStarter INTEGER NOT NULL,
                LegacyDiscography INTEGER NOT NULL,
                StartedByUserValidated INTEGER NULL,
                IsHidden INTEGER NOT NULL DEFAULT 0,
                StarterAttachment TEXT NULL,
                StarterFileSize TEXT NULL,
                StarterAttachCount INTEGER NOT NULL,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES ModernForumCategory (Id)
            );
            """);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
