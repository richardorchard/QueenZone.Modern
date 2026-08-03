using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

/// <summary>
/// SQLite materialization checks for public-read repositories that project legacy rows.
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
                PIC_WIDTH INTEGER NOT NULL,
                PIC_HEIGHT INTEGER NOT NULL,
                pic_id INTEGER NOT NULL,
                category_name TEXT,
                cat_id INTEGER NOT NULL
            );
            CREATE TABLE FreddieTributes (
                Id INTEGER NOT NULL,
                Name TEXT,
                Thought TEXT,
                Email TEXT,
                DateText TEXT NOT NULL,
                TimeText TEXT,
                Country TEXT,
                Display INTEGER
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
                """,
            userId => $"""
                SELECT USER_ID, USERNAME FROM UsersLookup WHERE USER_ID = {userId} LIMIT 1
                """);

        var match = await repository.FindByEmailAsync("freddie@example.com");
        Assert.NotNull(match);
        Assert.Equal(42, match.UserId);
        Assert.Equal("Freddie", match.Username);

        Assert.Null(await repository.FindByEmailAsync("missing@example.com"));

        var byId = await repository.FindByUserIdAsync(42);
        Assert.NotNull(byId);
        Assert.Equal("Freddie", byId.Username);
        Assert.Null(await repository.FindByUserIdAsync(999));
    }

    [Fact]
    public async Task Photo_maps_categories_paging_navigation_and_sitemap_without_full_collection()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO PhotoCategories (cat_id, name) VALUES (3, 'Live 1986');
            INSERT INTO PhotoItems (NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, PIC_WIDTH, PIC_HEIGHT, pic_id, category_name, cat_id)
            VALUES
                ('Newest', '1986-07-12 00:00:00', 'n.jpg', 'n-t.jpg', 100, 150, 1920, 1080, 11, 'Live 1986', 3),
                ('Middle', '1986-07-11 00:00:00', 'm.jpg', 'm-t.jpg', 100, 150, 800, 600, 10, 'Live 1986', 3),
                ('Oldest', '1986-07-10 00:00:00', 'o.jpg', 'o-t.jpg', 100, 150, 0, 0, 9, 'Live 1986', 3);
            """);

        var repository = new EfPhotoRepository(dbContext, PhotoSqlQueries.CreateSqliteFixture());

        var categories = await repository.GetCategoriesAsync();
        Assert.Single(categories);
        Assert.Equal(3, categories[0].CatId);
        Assert.Equal(3, categories[0].ImageCount);
        Assert.NotNull(categories[0].CoverThumbnailUrl);

        var bySlug = await repository.GetCategoryBySlugAsync("live-1986");
        Assert.NotNull(bySlug);

        var page = await repository.GetCategoryPageAsync(3, 1, 2);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(11, page.Items[0].PicId);
        Assert.Equal(1920, page.Items[0].PictureWidth);
        Assert.Equal(1080, page.Items[0].PictureHeight);
        Assert.Equal("1920 x 1080", page.Items[0].PictureDimensionsLabel);
        Assert.Equal(10, page.Items[1].PicId);

        var page2 = await repository.GetCategoryPageAsync(3, 2, 2);
        Assert.Single(page2.Items);
        Assert.Equal(9, page2.Items[0].PicId);
        Assert.False(page2.Items[0].HasPictureDimensions);
        Assert.Null(page2.Items[0].PictureDimensionsLabel);

        var middle = await repository.GetDetailNavigationAsync(3, 10);
        Assert.NotNull(middle);
        Assert.Equal(1, middle.Index);
        Assert.Equal(3, middle.Count);
        Assert.Equal(11, middle.PreviousPicId);
        Assert.Equal(9, middle.NextPicId);
        Assert.Equal(800, middle.Photo.PictureWidth);
        Assert.Equal(600, middle.Photo.PictureHeight);

        var newest = await repository.GetDetailNavigationAsync(3, 11);
        Assert.NotNull(newest);
        Assert.Equal(0, newest.Index);
        Assert.Null(newest.PreviousPicId);
        Assert.Equal(10, newest.NextPicId);

        Assert.Null(await repository.GetDetailNavigationAsync(3, 9999));
        Assert.Null(await repository.GetDetailNavigationAsync(99, 11));

        var all = await repository.GetCategoryAllAsync(3);
        Assert.Equal(3, all.Count);

        var sitemap = await repository.GetPublishedSitemapCategoriesAsync();
        Assert.Single(sitemap);
        Assert.Equal(3, sitemap[0].CatId);
        Assert.Equal("live-1986", sitemap[0].Slug);
        Assert.Equal(3, sitemap[0].Photos.Count);
        Assert.Equal(11, sitemap[0].Photos[0].PicId);
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

        // List SQL projects empty Body (no ARTICLE_TEXT LOB) — mapping must still work.
        const string listSelect = """
            SELECT Id, Title, CAST('' AS TEXT) AS Body, PublishedAt, Source, CategoryName, IsPublished
            FROM Articles
            WHERE IsPublished = 1
            """;
        const string detailSelect = """
            SELECT Id, Title, Body, PublishedAt, Source, CategoryName, IsPublished
            FROM Articles
            WHERE IsPublished = 1
            """;

        var repository = new EfArticlesRepository(
            dbContext,
            latestSql: listSelect + " ORDER BY PublishedAt DESC, Id DESC LIMIT {0}",
            countSql: "SELECT COUNT(*) AS Value FROM Articles WHERE IsPublished = 1",
            archivePageSql: listSelect + " ORDER BY PublishedAt DESC, Id DESC LIMIT {1} OFFSET {0}",
            byIdSql: detailSelect + " AND Id = {0}",
            sitemapSql: """
                SELECT Id, Title, PublishedAt, CAST(NULL AS TEXT) AS Slug
                FROM Articles WHERE IsPublished = 1
                ORDER BY PublishedAt DESC, Id DESC
                """);

        var latest = await repository.GetLatestAsync(10);
        Assert.Equal(2, latest.Count);
        Assert.Equal("Second", latest[0].Title);
        // List mapping does not require body; Body is always empty on list items.
        Assert.Equal(string.Empty, latest[0].Body);
        Assert.Equal(string.Empty, latest[1].Body);

        Assert.Equal(2, await repository.GetPublishedCountAsync());

        var page = await repository.GetArchivePageAsync(1, 1);
        Assert.Single(page);
        Assert.Equal(2, page[0].Id);
        Assert.Equal(string.Empty, page[0].Body);

        var detail = await repository.GetByIdAsync(1);
        Assert.NotNull(detail);
        Assert.Equal("First", detail.Title);
        Assert.Equal("Body one is long enough for an excerpt", detail.Body);
        Assert.False(string.IsNullOrWhiteSpace(detail.Excerpt));
        Assert.Null(await repository.GetByIdAsync(999));

        var sitemap = await repository.GetPublishedSitemapEntriesAsync();
        Assert.Equal(2, sitemap.Count);
    }

    [Fact]
    public async Task Articles_list_with_body_preview_derives_excerpt_but_clears_body()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS ArticlesPreview (
                Id INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Body TEXT NOT NULL,
                PublishedAt TEXT NOT NULL,
                Source TEXT,
                CategoryName TEXT,
                IsPublished INTEGER NOT NULL
            );
            INSERT INTO ArticlesPreview (Id, Title, Body, PublishedAt, Source, CategoryName, IsPublished)
            VALUES
                (1, 'Previewed', 'Body one is long enough for an excerpt', '2020-01-01', 'BBC', 'News', 1);
            """);

        // Mirrors production list: truncated Body used only for excerpt, then discarded.
        const string listSelect = """
            SELECT Id, Title, substr(Body, 1, 2000) AS Body, PublishedAt, Source, CategoryName, IsPublished
            FROM ArticlesPreview
            WHERE IsPublished = 1
            """;

        var repository = new EfArticlesRepository(
            dbContext,
            latestSql: listSelect + " ORDER BY PublishedAt DESC, Id DESC LIMIT {0}",
            countSql: "SELECT COUNT(*) AS Value FROM ArticlesPreview WHERE IsPublished = 1",
            archivePageSql: listSelect + " ORDER BY PublishedAt DESC, Id DESC LIMIT {1} OFFSET {0}",
            byIdSql: listSelect + " AND Id = {0}",
            sitemapSql: """
                SELECT Id, Title, PublishedAt, CAST(NULL AS TEXT) AS Slug
                FROM ArticlesPreview WHERE IsPublished = 1
                """);

        var latest = await repository.GetLatestAsync(1);
        Assert.Single(latest);
        Assert.False(string.IsNullOrWhiteSpace(latest[0].Excerpt));
        Assert.Equal(string.Empty, latest[0].Body);
    }

    [Fact]
    public async Task FreddieTributes_maps_public_page_and_random_without_email()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO FreddieTributes (Id, Name, Thought, Email, DateText, TimeText, Country, Display)
            VALUES
                (1, 'Hidden', 'Private note', 'hidden@example.test', '24 November 2001', '08:00', 'UK', 0),
                (2, 'Blank', '', 'blank@example.test', '24 November 2001', '09:00', 'UK', 1),
                (3, '  Maya  ', '  Freddie still shines.  ', 'maya@example.test', '24 November 2001', '10:00', 'India', 1),
                (4, NULL, 'Anonymous love for Freddie.', 'anon@example.test', '24 November 2001', NULL, NULL, 1);
            """);

        var repository = new EfFreddieTributeRepository(
            dbContext,
            pageSql: """
                SELECT
                    Id,
                    COALESCE(TRIM(Name), 'Anonymous') AS Name,
                    TRIM(Thought) AS Thought,
                    NULLIF(TRIM(COALESCE(Country, '')), '') AS Country,
                    TRIM(DateText) AS DateText,
                    NULLIF(TRIM(COALESCE(TimeText, '')), '') AS TimeText
                FROM FreddieTributes
                WHERE Display = 1 AND NULLIF(TRIM(COALESCE(Thought, '')), '') IS NOT NULL
                ORDER BY Id DESC
                LIMIT {1} OFFSET {0}
                """,
            countSql: """
                SELECT COUNT(*) AS Value
                FROM FreddieTributes
                WHERE Display = 1 AND NULLIF(TRIM(COALESCE(Thought, '')), '') IS NOT NULL
                """,
            randomSql: """
                SELECT
                    Id,
                    COALESCE(TRIM(Name), 'Anonymous') AS Name,
                    TRIM(Thought) AS Thought,
                    NULLIF(TRIM(COALESCE(Country, '')), '') AS Country,
                    TRIM(DateText) AS DateText,
                    NULLIF(TRIM(COALESCE(TimeText, '')), '') AS TimeText
                FROM FreddieTributes
                WHERE Display = 1 AND NULLIF(TRIM(COALESCE(Thought, '')), '') IS NOT NULL
                ORDER BY Id DESC
                LIMIT 1
                """);

        var page = await repository.GetPageAsync(1, 10);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(4, page.Items[0].Id);
        Assert.Equal("Anonymous", page.Items[0].Name);
        Assert.Equal("Maya", page.Items[1].Name);
        Assert.Equal("Freddie still shines.", page.Items[1].Thought);
        Assert.Equal("India", page.Items[1].Country);
        Assert.Equal("10:00", page.Items[1].TimeText);

        var random = await repository.GetRandomAsync();
        Assert.NotNull(random);
        Assert.Equal(4, random.Id);
    }

    [Fact]
    public async Task AdminFreddieTributes_maps_filters_duplicate_counts_and_detail()
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO FreddieTributes (Id, Name, Thought, Email, DateText, TimeText, Country, Display)
            VALUES
                (20, 'Visible', 'Repeated', 'visible@example.test', '24 November 2001', '08:00', 'UK', 1),
                (21, 'Visible', 'Repeated', 'visible2@example.test', '24 November 2001', '08:01', 'UK', 1),
                (22, 'Hidden', 'Filtered', 'hidden@example.test', '24 November 2001', NULL, 'US', 0);
            """);

        const string pageSql = """
            WITH Filtered AS
            (
                SELECT
                    Id,
                    COALESCE(TRIM(Name), 'Anonymous') AS Name,
                    TRIM(Thought) AS Thought,
                    NULLIF(TRIM(COALESCE(Country, '')), '') AS Country,
                    TRIM(DateText) AS DateText,
                    NULLIF(TRIM(COALESCE(TimeText, '')), '') AS TimeText,
                    Display = 1 AS IsVisible,
                    COUNT(*) OVER (
                        PARTITION BY UPPER(TRIM(COALESCE(Name, ''))), UPPER(TRIM(COALESCE(Thought, '')))
                    ) AS DuplicateCount
                FROM FreddieTributes
                WHERE ({2} IS NULL OR Display = {2})
                  AND ({3} IS NULL OR Name LIKE '%' || {3} || '%' OR Thought LIKE '%' || {3} || '%' OR Country LIKE '%' || {3} || '%')
            )
            SELECT Id, Name, Thought, Country, DateText, TimeText, IsVisible, DuplicateCount
            FROM Filtered
            WHERE ({4} = 0 OR DuplicateCount > 1)
            ORDER BY Id DESC
            LIMIT {1} OFFSET {0}
            """;
        const string countSql = """
            WITH Filtered AS
            (
                SELECT
                    Id,
                    COUNT(*) OVER (
                        PARTITION BY UPPER(TRIM(COALESCE(Name, ''))), UPPER(TRIM(COALESCE(Thought, '')))
                    ) AS DuplicateCount
                FROM FreddieTributes
                WHERE ({0} IS NULL OR Display = {0})
                  AND ({1} IS NULL OR Name LIKE '%' || {1} || '%' OR Thought LIKE '%' || {1} || '%' OR Country LIKE '%' || {1} || '%')
            )
            SELECT COUNT(*) AS Value
            FROM Filtered
            WHERE ({2} = 0 OR DuplicateCount > 1)
            """;
        const string byIdSql = """
            SELECT
                Id,
                COALESCE(TRIM(Name), 'Anonymous') AS Name,
                TRIM(Thought) AS Thought,
                NULLIF(TRIM(COALESCE(Country, '')), '') AS Country,
                TRIM(DateText) AS DateText,
                NULLIF(TRIM(COALESCE(TimeText, '')), '') AS TimeText,
                Display = 1 AS IsVisible,
                COUNT(*) OVER (
                    PARTITION BY UPPER(TRIM(COALESCE(Name, ''))), UPPER(TRIM(COALESCE(Thought, '')))
                ) AS DuplicateCount
            FROM FreddieTributes
            WHERE Id = {0}
            """;

        var repository = new EfAdminFreddieTributeRepository(dbContext, pageSql, countSql, byIdSql);

        var duplicates = await repository.GetPageAsync(
            new AdminFreddieTributeListFilter(true, "Repeated", DuplicatesOnly: true),
            1,
            10);

        Assert.Equal(2, duplicates.TotalCount);
        Assert.Equal(21, duplicates.Items[0].Id);
        Assert.True(duplicates.Items[0].IsVisible);
        Assert.Equal(2, duplicates.Items[0].DuplicateCount);

        var hidden = await repository.GetByIdAsync(22);
        Assert.NotNull(hidden);
        Assert.False(hidden.IsVisible);
        Assert.Null(hidden.TimeText);
    }

    [Fact]
    public void Article_production_sql_splits_list_and_detail_body_projections()
    {
        var queries = EfProductionSql.CreateArticlesQueries();

        Assert.Contains(" AS Slug", queries.Sitemap, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ARTICLE_TEXT", queries.Sitemap, StringComparison.OrdinalIgnoreCase);

        // List/archive: truncated preview only (not full body LOB).
        Assert.Contains(
            $"LEFT(ISNULL(CAST(a.ARTICLE_TEXT AS nvarchar(max)), N''), {EfProductionSql.ArticlesListBodyPreviewChars})",
            queries.Latest,
            StringComparison.Ordinal);
        Assert.Contains(
            $"LEFT(ISNULL(CAST(a.ARTICLE_TEXT AS nvarchar(max)), N''), {EfProductionSql.ArticlesListBodyPreviewChars})",
            queries.ArchivePage,
            StringComparison.Ordinal);
        Assert.DoesNotContain("LEFT(ISNULL(a.ARTICLE_TEXT", queries.Latest, StringComparison.Ordinal);
        Assert.DoesNotContain("LEFT(ISNULL(a.ARTICLE_TEXT", queries.ArchivePage, StringComparison.Ordinal);
        Assert.DoesNotContain("ISNULL(a.ARTICLE_TEXT, '') AS Body", queries.Latest, StringComparison.Ordinal);
        Assert.DoesNotContain("ISNULL(a.ARTICLE_TEXT, '') AS Body", queries.ArchivePage, StringComparison.Ordinal);

        // Detail still loads full body.
        Assert.Contains("ISNULL(CAST(a.ARTICLE_TEXT AS nvarchar(max)), N'') AS Body", queries.ById, StringComparison.Ordinal);
        Assert.DoesNotContain("LEFT(ISNULL(a.ARTICLE_TEXT", queries.ById, StringComparison.Ordinal);
    }

    [Fact]
    public void News_and_article_production_sql_use_parameter_placeholders()
    {
        var news = EfProductionSql.CreateNewsQueries("/*list*/", "/*detail*/");
        Assert.Contains("{0}", news.Latest, StringComparison.Ordinal);
        Assert.Contains("{0}", news.ArchivePage, StringComparison.Ordinal);
        Assert.Contains("{1}", news.ArchivePage, StringComparison.Ordinal);
        Assert.Contains("{0}", news.ById, StringComparison.Ordinal);
        Assert.Contains("/*list*/", news.Latest, StringComparison.Ordinal);
        Assert.Contains("/*detail*/", news.ById, StringComparison.Ordinal);
        Assert.DoesNotContain("/*detail*/", news.Latest, StringComparison.Ordinal);
        Assert.DoesNotContain("/*list*/", news.ById, StringComparison.Ordinal);

        var articles = EfProductionSql.CreateArticlesQueries();
        Assert.Contains("{0}", articles.Latest, StringComparison.Ordinal);
        Assert.Contains("{0}", articles.ArchivePage, StringComparison.Ordinal);
        Assert.Contains("{1}", articles.ArchivePage, StringComparison.Ordinal);
        Assert.Contains("{0}", articles.ById, StringComparison.Ordinal);
    }

    [Fact]
    public void News_production_sql_omits_article_lob_from_list_and_sitemap()
    {
        var listCte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlugColumn: true, includeBody: false);
        var detailCte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlugColumn: true, includeBody: true);
        var news = EfProductionSql.CreateNewsQueries(listCte, detailCte);

        // List CTE never touches ARTICLE; empty Body constant for EF materialization only.
        Assert.DoesNotContain("ARTICLE", listCte, StringComparison.Ordinal);
        Assert.Contains("CAST(N'' AS nvarchar(max)) AS Body", listCte, StringComparison.Ordinal);
        Assert.Contains("ISNULL(ARTICLE, '') AS Body", detailCte, StringComparison.Ordinal);

        Assert.DoesNotContain("ARTICLE", news.Latest, StringComparison.Ordinal);
        Assert.DoesNotContain("ARTICLE", news.ArchivePage, StringComparison.Ordinal);
        Assert.DoesNotContain("ARTICLE", news.Sitemap, StringComparison.Ordinal);
        Assert.DoesNotContain("ARTICLE", news.Count, StringComparison.Ordinal);

        // Detail still projects the real ARTICLE body.
        Assert.Contains("ISNULL(ARTICLE, '') AS Body", news.ById, StringComparison.Ordinal);
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
                (10, 'Headline', 'Ex', 'Full body text for detail', '2021-03-01', 'https://example.com', 1, 'headline'),
                (11, 'Other', 'Ex2', 'Body2', '2021-04-01', NULL, 1, NULL);
            """);

        // List SQL projects empty Body (no ARTICLE LOB) — mapping must not require real body text.
        const string listSelect = """
            SELECT Id, Title, Excerpt, CAST('' AS TEXT) AS Body, PublishedAt, SourceUrl, IsPublished, Slug
            FROM NewsRows
            WHERE IsPublished = 1
            """;
        const string detailSelect = """
            SELECT Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug
            FROM NewsRows
            WHERE IsPublished = 1
            """;

        var repository = new EfNewsRepository(
            dbContext,
            latestSql: listSelect + " ORDER BY PublishedAt DESC, Id DESC LIMIT {0}",
            countSql: "SELECT COUNT(*) AS Value FROM NewsRows WHERE IsPublished = 1",
            archivePageSql: listSelect + " ORDER BY PublishedAt DESC, Id DESC LIMIT {1} OFFSET {0}",
            byIdSql: detailSelect + " AND Id = {0}",
            sitemapSql: """
                SELECT Id, Title, PublishedAt, Slug FROM NewsRows WHERE IsPublished = 1
                ORDER BY PublishedAt DESC, Id DESC
                """);

        var latest = await repository.GetLatestAsync(5);
        Assert.Equal(2, latest.Count);
        Assert.Equal(11, latest[0].Id);
        Assert.Equal("Ex2", latest[0].Excerpt);
        Assert.Equal(string.Empty, latest[0].Body);
        Assert.Equal(string.Empty, latest[1].Body);

        Assert.Equal(2, await repository.GetPublishedCountAsync());
        var page = await repository.GetArchivePageAsync(1, 1);
        Assert.Single(page);
        Assert.Equal(string.Empty, page[0].Body);

        var detail = await repository.GetByIdAsync(10);
        Assert.NotNull(detail);
        Assert.Equal("Headline", detail.Title);
        Assert.Equal("Full body text for detail", detail.Body);
        Assert.Null(await repository.GetByIdAsync(404));
        Assert.Equal(2, (await repository.GetPublishedSitemapEntriesAsync()).Count);
    }

}
