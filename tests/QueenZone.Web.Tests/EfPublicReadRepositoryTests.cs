using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

/// <summary>
/// SQLite materialization coverage for EF public-read repositories that used Dapper.
/// Production paths use SQL Server procs/SQL; tests substitute SELECT shapes.
/// </summary>
public sealed class EfPublicReadRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;

    public EfPublicReadRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE AlbumList (
                Q_ALBUM_ID INTEGER NOT NULL,
                ALBUM_NAME TEXT NOT NULL,
                release_year INTEGER,
                thumb_url TEXT
            );
            CREATE TABLE AlbumDisplay (
                Q_ALBUM_ID INTEGER NOT NULL,
                ALBUM_NAME TEXT NOT NULL,
                RELEASE_DATE TEXT,
                GENERAL_NOTES TEXT,
                ARTIST_NAME TEXT NOT NULL,
                THUMB_URL TEXT,
                PICTURE_URL TEXT,
                ACTIVE INTEGER NOT NULL
            );
            CREATE TABLE AlbumSong (
                Q_ALBUM_SONG_ID INTEGER NOT NULL,
                SONG_TITLE TEXT NOT NULL,
                IS_SINGLE INTEGER NOT NULL,
                SONG_LYRICS TEXT,
                SONG_NOTES TEXT
            );
            CREATE TABLE StageList (
                Q_STAGE_ID INTEGER NOT NULL,
                TITLE TEXT NOT NULL,
                PERFORMED_BY TEXT NOT NULL,
                DESCRIPTION TEXT,
                URL TEXT NOT NULL,
                thesize TEXT,
                DATE_ADDED TEXT NOT NULL
            );
            CREATE TABLE UsersLookup (
                USER_ID INTEGER NOT NULL,
                USERNAME TEXT,
                EMAIL TEXT NOT NULL
            );
            CREATE TABLE ArticleArchive (
                Id INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Body TEXT NOT NULL,
                PublishedAt TEXT NOT NULL,
                Source TEXT,
                CategoryName TEXT,
                IsPublished INTEGER NOT NULL
            );
            CREATE TABLE PhotoCategories (
                cat_id INTEGER NOT NULL,
                name TEXT NOT NULL
            );
            CREATE TABLE PhotoItems (
                NAME TEXT NOT NULL,
                DATE_TIME TEXT NOT NULL,
                URL TEXT NOT NULL,
                THUMB_URL TEXT NOT NULL,
                T_HEIGHT INTEGER NOT NULL,
                T_WIDTH INTEGER NOT NULL,
                pic_id INTEGER NOT NULL,
                category_name TEXT,
                cat_id INTEGER NOT NULL
            );
            """);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Discography_maps_list_and_active_album_with_songs()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO AlbumList (Q_ALBUM_ID, ALBUM_NAME, release_year, thumb_url)
            VALUES (1, 'A Night at the Opera', 1975, 'opera.jpg');
            INSERT INTO AlbumDisplay (Q_ALBUM_ID, ALBUM_NAME, RELEASE_DATE, GENERAL_NOTES, ARTIST_NAME, THUMB_URL, PICTURE_URL, ACTIVE)
            VALUES (1, 'A Night at the Opera', '1975-11-21', 'Notes', 'Queen', 't.jpg', 'p.jpg', 1);
            INSERT INTO AlbumDisplay (Q_ALBUM_ID, ALBUM_NAME, RELEASE_DATE, GENERAL_NOTES, ARTIST_NAME, THUMB_URL, PICTURE_URL, ACTIVE)
            VALUES (2, 'Hidden', '1970-01-01', NULL, 'Queen', NULL, NULL, 0);
            INSERT INTO AlbumSong (Q_ALBUM_SONG_ID, SONG_TITLE, IS_SINGLE, SONG_LYRICS, SONG_NOTES)
            VALUES (10, 'Bohemian Rhapsody', 1, 'Is this the real life', 'Single');
            """);

        var repository = new EfDiscographyRepository(
            dbContext,
            listSql: "SELECT Q_ALBUM_ID, ALBUM_NAME, release_year, thumb_url FROM AlbumList",
            displaySql: id => $"""
                SELECT Q_ALBUM_ID, ALBUM_NAME, RELEASE_DATE, GENERAL_NOTES, ARTIST_NAME, THUMB_URL, PICTURE_URL, ACTIVE
                FROM AlbumDisplay WHERE Q_ALBUM_ID = {id}
                """,
            songsSql: id => $"""
                SELECT Q_ALBUM_SONG_ID, SONG_TITLE, IS_SINGLE, SONG_LYRICS, SONG_NOTES
                FROM AlbumSong
                """);

        var albums = await repository.GetAlbumsAsync();
        Assert.Single(albums);
        Assert.Equal(1, albums[0].AlbumId);
        Assert.Equal("a-night-at-the-opera", albums[0].Slug);
        Assert.Equal(1975, albums[0].ReleaseYear);

        var detail = await repository.GetAlbumByIdAsync(1);
        Assert.NotNull(detail);
        Assert.Equal("Queen", detail.ArtistName);
        Assert.Single(detail.Songs);
        Assert.True(detail.Songs[0].IsSingle);

        Assert.Null(await repository.GetAlbumByIdAsync(2));
        Assert.Null(await repository.GetAlbumByIdAsync(99));
    }

    [Fact]
    public async Task FanPerformance_maps_page_count_and_detail()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO StageList (Q_STAGE_ID, TITLE, PERFORMED_BY, DESCRIPTION, URL, thesize, DATE_ADDED)
            VALUES (5, 'Show Must Go On', 'Fan Band', 'Cover', 'show.mp3', '1024', '2020-01-02');
            """);

        var repository = new EfFanPerformanceRepository(
            dbContext,
            useLegacyProcedures: false,
            pageSelectSql: """
                SELECT Q_STAGE_ID, TITLE, PERFORMED_BY, DESCRIPTION, URL, thesize, DATE_ADDED FROM StageList
                """,
            countSql: "SELECT COUNT(*) AS Value FROM StageList",
            byIdSql: id => $"""
                SELECT Q_STAGE_ID, TITLE, PERFORMED_BY, DESCRIPTION, URL, thesize, DATE_ADDED
                FROM StageList WHERE Q_STAGE_ID = {id}
                """);

        var page = await repository.GetPageAsync(1, 10);
        Assert.Single(page);
        Assert.Equal(5, page[0].Id);
        Assert.Equal(1024, page[0].FileSizeBytes);

        Assert.Equal(1, await repository.GetVisibleCountAsync());
        Assert.NotNull(await repository.GetByIdAsync(5));
        Assert.Null(await repository.GetByIdAsync(999));
    }

    [Fact]
    public async Task MemberLookup_finds_by_email()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO UsersLookup (USER_ID, USERNAME, EMAIL)
            VALUES (42, '  Freddie  ', 'freddie@example.com');
            """);

        var repository = new EfMemberLookupRepository(
            dbContext,
            email => $"""
                SELECT USER_ID, USERNAME FROM UsersLookup WHERE EMAIL = {email} LIMIT 1
                """);

        var match = await repository.FindByEmailAsync("freddie@example.com");
        Assert.NotNull(match);
        Assert.Equal(42, match.UserId);
        Assert.Equal("Freddie", match.Username);

        Assert.Null(await repository.FindByEmailAsync("missing@example.com"));
    }

    [Fact]
    public async Task Photo_maps_categories_and_collection_without_procs()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO PhotoCategories (cat_id, name) VALUES (3, 'Live 1986');
            INSERT INTO PhotoItems (NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, pic_id, category_name, cat_id)
            VALUES ('Crowd', '1986-07-12', 'crowd.jpg', 'crowd-t.jpg', 100, 150, 9, 'Live 1986', 3);
            """);

        var repository = new EfPhotoRepository(
            dbContext,
            useLegacyProcedures: false,
            categoriesSql: "SELECT cat_id, name FROM PhotoCategories",
            categoryPageSql: catId => $"""
                SELECT NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, pic_id, category_name
                FROM PhotoItems WHERE cat_id = {catId}
                """.ToString());

        // FormattableString.ToString() expands params - use raw format with literal id for SQLite test:
        repository = new EfPhotoRepository(
            dbContext,
            useLegacyProcedures: false,
            categoriesSql: "SELECT cat_id, name FROM PhotoCategories",
            categoryPageSql: catId =>
                "SELECT NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, pic_id, category_name " +
                $"FROM PhotoItems WHERE cat_id = {catId}");

        var categories = await repository.GetCategoriesAsync();
        Assert.Single(categories);
        Assert.Equal(3, categories[0].CatId);
        Assert.Equal(1, categories[0].ImageCount);

        var bySlug = await repository.GetCategoryBySlugAsync("live-1986");
        Assert.NotNull(bySlug);

        var page = await repository.GetCategoryPageAsync(3, 1, 10);
        Assert.Single(page.Items);
        Assert.Equal(9, page.Items[0].PicId);

        var all = await repository.GetCategoryAllAsync(3);
        Assert.Single(all);
    }

    [Fact]
    public async Task Articles_maps_latest_page_count_detail_and_sitemap()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS Articles (
                Id INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Body TEXT NOT NULL,
                PublishedAt TEXT NOT NULL,
                Source TEXT,
                CategoryName TEXT,
                IsPublished INTEGER NOT NULL
            );
            INSERT INTO Articles (Id, Title, Body, PublishedAt, Source, CategoryName, IsPublished)
            VALUES
                (1, 'First', 'Body one is long enough for an excerpt', '2020-01-01', 'BBC', 'News', 1),
                (2, 'Second', 'Body two', '2020-02-01', NULL, NULL, 1);
            """);

        const string select = """
            SELECT Id, Title, Body, PublishedAt, Source, CategoryName, IsPublished
            FROM Articles
            WHERE IsPublished = 1
            """;

        var repository = new EfArticlesRepository(
            dbContext,
            latestSql: count => select + $" ORDER BY PublishedAt DESC, Id DESC LIMIT {count}",
            countSql: "SELECT COUNT(*) AS Value FROM Articles WHERE IsPublished = 1",
            archivePageSql: (offset, pageSize) =>
                select + $" ORDER BY PublishedAt DESC, Id DESC LIMIT {pageSize} OFFSET {offset}",
            byIdSql: id => select + $" AND Id = {id}",
            sitemapSql: """
                SELECT Id, Title, PublishedAt, CAST(NULL AS TEXT) AS Slug
                FROM Articles WHERE IsPublished = 1
                ORDER BY PublishedAt DESC, Id DESC
                """);

        var latest = await repository.GetLatestAsync(10);
        Assert.Equal(2, latest.Count);
        Assert.Equal("Second", latest[0].Title);
        Assert.False(string.IsNullOrWhiteSpace(latest[0].Excerpt));

        Assert.Equal(2, await repository.GetPublishedCountAsync());

        var page = await repository.GetArchivePageAsync(1, 1);
        Assert.Single(page);
        Assert.Equal(2, page[0].Id);

        var detail = await repository.GetByIdAsync(1);
        Assert.NotNull(detail);
        Assert.Equal("First", detail.Title);
        Assert.Null(await repository.GetByIdAsync(999));

        var sitemap = await repository.GetPublishedSitemapEntriesAsync();
        Assert.Equal(2, sitemap.Count);
    }

    [Fact]
    public async Task News_maps_latest_page_count_detail_and_sitemap()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS NewsRows (
                Id INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Excerpt TEXT NOT NULL,
                Body TEXT NOT NULL,
                PublishedAt TEXT NOT NULL,
                SourceUrl TEXT,
                IsPublished INTEGER NOT NULL,
                Slug TEXT
            );
            INSERT INTO NewsRows (Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug)
            VALUES
                (10, 'Headline', 'Ex', 'Body', '2021-03-01', 'https://example.com', 1, 'headline'),
                (11, 'Other', 'Ex2', 'Body2', '2021-04-01', NULL, 1, NULL);
            """);

        const string select = """
            SELECT Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug
            FROM NewsRows
            WHERE IsPublished = 1
            """;

        var repository = new EfNewsRepository(
            dbContext,
            latestSql: count => select + $" ORDER BY PublishedAt DESC, Id DESC LIMIT {count}",
            countSql: "SELECT COUNT(*) AS Value FROM NewsRows WHERE IsPublished = 1",
            archivePageSql: (offset, pageSize) =>
                select + $" ORDER BY PublishedAt DESC, Id DESC LIMIT {pageSize} OFFSET {offset}",
            byIdSql: id => select + $" AND Id = {id}",
            sitemapSql: """
                SELECT Id, Title, PublishedAt, Slug FROM NewsRows WHERE IsPublished = 1
                ORDER BY PublishedAt DESC, Id DESC
                """);

        var latest = await repository.GetLatestAsync(5);
        Assert.Equal(2, latest.Count);
        Assert.Equal(11, latest[0].Id);

        Assert.Equal(2, await repository.GetPublishedCountAsync());
        Assert.Single(await repository.GetArchivePageAsync(1, 1));
        Assert.Equal("Headline", (await repository.GetByIdAsync(10))!.Title);
        Assert.Null(await repository.GetByIdAsync(404));
        Assert.Equal(2, (await repository.GetPublishedSitemapEntriesAsync()).Count);
    }

    [Fact]
    public void Public_constructors_accept_dbContext()
    {
        Assert.NotNull(new EfDiscographyRepository(dbContext));
        Assert.NotNull(new EfFanPerformanceRepository(dbContext));
        Assert.NotNull(new EfPhotoRepository(dbContext));
        Assert.NotNull(new EfMemberLookupRepository(dbContext));
        Assert.NotNull(new EfArticlesRepository(dbContext));
        Assert.NotNull(new ModernForumRepository(dbContext));
        Assert.NotNull(new LegacyForumRepository(dbContext));
    }

    [Fact]
    public void News_public_constructor_requires_connection_string()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var bareContext = new QueenZoneDbContext(options);
        Assert.ThrowsAny<Exception>(() => new EfNewsRepository(bareContext));
    }
}
