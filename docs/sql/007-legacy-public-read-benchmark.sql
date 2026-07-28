/*
  QueenZone selective legacy→modern evaluation benchmark (issue #334)

  Purpose
  -------
  Read-only timings for public archive SQL shapes that still hit legacy tables:
  NEWS_T latest-row CTE (PublishedNewsQuery), articles list, photography pages,
  fan performances, biography/album procs. Use results to decide whether a
  forum-style modern projection is justified.

  This script does not create or modify database objects.

  Suggested usage (from repo root, after loading ConnectionStrings__QueenZoneLegacy):

      powershell -File .\scripts\Run-LegacyPublicReadBenchmark.ps1

  Or sqlcmd with a trusted connection string (do not commit credentials):

      sqlcmd -C -b -I -t 0 -i docs/sql/007-legacy-public-read-benchmark.sql

  Notes
  -----
  - Client tools add network RTT. Prefer the "server ms" section (DATEDIFF) for
    SQL engine cost; use client wall-clock for end-to-end from a remote laptop.
  - Forum multi-second legacy path was already modernized; see
    docs/sql/005-forum-read-performance-benchmark.sql and
    docs/performance/forum-read-benchmark-2026-06-29.md.
  - Run more than once for warm-cache behavior.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Runs int = 3;
DECLARE @Results table
(
    RunNumber int NOT NULL,
    Area varchar(80) NOT NULL,
    Sample varchar(80) NOT NULL,
    RowsRead int NOT NULL,
    ElapsedMs int NOT NULL,
    Notes varchar(200) NULL
);

DECLARE @Run int = 1;
WHILE @Run <= @Runs
BEGIN
    DECLARE @StartedAt datetime2(7);
    DECLARE @Rows int;
    DECLARE @Cnt int;

    /* Inventory once (run 1 only) */
    IF @Run = 1
    BEGIN
        DECLARE @NewsAll bigint = (SELECT COUNT_BIG(*) FROM dbo.NEWS_T);
        DECLARE @NewsPub bigint = (SELECT COUNT_BIG(*) FROM dbo.NEWS_T WHERE DISPLAY = 1);
        DECLARE @NewsDistinct bigint = (SELECT COUNT_BIG(DISTINCT NEWS_ID) FROM dbo.NEWS_T);
        DECLARE @NewsDupGroups bigint = (
            SELECT COUNT_BIG(*) FROM (
                SELECT NEWS_ID FROM dbo.NEWS_T GROUP BY NEWS_ID HAVING COUNT(*) > 1
            ) d);
        DECLARE @HasNewsIx bit = CASE WHEN EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.NEWS_T') AND name = N'IX_NEWS_T_Display_Date'
        ) THEN 1 ELSE 0 END;

        INSERT @Results (RunNumber, Area, Sample, RowsRead, ElapsedMs, Notes)
        VALUES
            (0, 'inventory-NEWS_T-all', 'rows', CAST(@NewsAll AS int), 0, NULL),
            (0, 'inventory-NEWS_T-display1', 'rows', CAST(@NewsPub AS int), 0, NULL),
            (0, 'inventory-NEWS_T-distinct-id', 'rows', CAST(@NewsDistinct AS int), 0, NULL),
            (0, 'inventory-NEWS_T-dup-id-groups', 'groups', CAST(@NewsDupGroups AS int), 0,
                'ROW_NUMBER CTE only needed when > 0'),
            (0, 'inventory-IX_NEWS_T_Display_Date', 'present', CAST(@HasNewsIx AS int), 0, NULL);
    END;

    /* News published count — CTE (app path) */
    SET @StartedAt = SYSUTCDATETIME();
    ;WITH PublishedNews AS (
        SELECT
            NEWS_ID AS Id,
            ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
        FROM dbo.NEWS_T
        WHERE DISPLAY = 1
    )
    SELECT @Cnt = COUNT(*) FROM PublishedNews WHERE RowNumber = 1;
    INSERT @Results VALUES (
        @Run, 'news-published-count-cte', 'all published', 1,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()),
        CONCAT('count=', @Cnt));

    /* News published count — simple (no dedupe) */
    SET @StartedAt = SYSUTCDATETIME();
    SELECT @Cnt = COUNT(*) FROM dbo.NEWS_T WHERE DISPLAY = 1;
    INSERT @Results VALUES (
        @Run, 'news-published-count-simple', 'all published', 1,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()),
        CONCAT('count=', @Cnt));

    /* News archive page 1 — CTE, no body (matches list projection after #354) */
    SET @StartedAt = SYSUTCDATETIME();
    ;WITH PublishedNews AS (
        SELECT
            NEWS_ID AS Id,
            TITLE AS Title,
            ISNULL(EXCERPT, '') AS Excerpt,
            CAST(N'' AS nvarchar(max)) AS Body,
            [DATE] AS PublishedAt,
            SOURCE_URL AS SourceUrl,
            CAST(1 AS bit) AS IsPublished,
            SLUG AS Slug,
            ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
        FROM dbo.NEWS_T
        WHERE DISPLAY = 1
    )
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug
        FROM PublishedNews
        WHERE RowNumber = 1
        ORDER BY PublishedAt DESC, Id DESC
        OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
    ) page;
    INSERT @Results VALUES (
        @Run, 'news-archive-page1-cte', 'page 1 size 20', @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* News deep archive page — still small table */
    SET @StartedAt = SYSUTCDATETIME();
    ;WITH PublishedNews AS (
        SELECT
            NEWS_ID AS Id,
            TITLE AS Title,
            ISNULL(EXCERPT, '') AS Excerpt,
            CAST(N'' AS nvarchar(max)) AS Body,
            [DATE] AS PublishedAt,
            SOURCE_URL AS SourceUrl,
            CAST(1 AS bit) AS IsPublished,
            SLUG AS Slug,
            ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
        FROM dbo.NEWS_T
        WHERE DISPLAY = 1
    )
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug
        FROM PublishedNews
        WHERE RowNumber = 1
        ORDER BY PublishedAt DESC, Id DESC
        OFFSET 1980 ROWS FETCH NEXT 20 ROWS ONLY
    ) page;
    INSERT @Results VALUES (
        @Run, 'news-archive-page100-cte', 'offset 1980 size 20', @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* News sitemap — full published set, no body */
    SET @StartedAt = SYSUTCDATETIME();
    ;WITH PublishedNews AS (
        SELECT
            NEWS_ID AS Id,
            TITLE AS Title,
            [DATE] AS PublishedAt,
            SLUG AS Slug,
            ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
        FROM dbo.NEWS_T
        WHERE DISPLAY = 1
    )
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT Id, Title, PublishedAt, Slug
        FROM PublishedNews
        WHERE RowNumber = 1
    ) s;
    INSERT @Results VALUES (
        @Run, 'news-sitemap-all-cte', 'all published titles', @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* Admin latest count (no DISPLAY filter) */
    SET @StartedAt = SYSUTCDATETIME();
    ;WITH LatestNews AS (
        SELECT
            NEWS_ID,
            ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
        FROM dbo.NEWS_T
    )
    SELECT @Cnt = COUNT(*) FROM LatestNews WHERE RowNumber = 1;
    INSERT @Results VALUES (
        @Run, 'news-admin-count-cte', 'all rows latest', 1,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()),
        CONCAT('count=', @Cnt));

    /* Articles archive page 1 with preview LOB slice */
    SET @StartedAt = SYSUTCDATETIME();
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT
            CAST(a.Q_ARTICLE_ID AS int) AS Id,
            a.ARTICLE_NAME AS Title,
            LEFT(ISNULL(CAST(a.ARTICLE_TEXT AS nvarchar(max)), N''), 2000) AS Body,
            a.DATE_CREATED AS PublishedAt
        FROM dbo.Q_ARTICLE_T a
        WHERE a.DISPLAY = 1
        ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
        OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
    ) page;
    INSERT @Results VALUES (
        @Run, 'articles-archive-page1-preview', 'page 1 size 20', @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* Photography category list with counts */
    SET @StartedAt = SYSUTCDATETIME();
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT c.cat_id, c.name, COUNT(p.PIC_ID) AS ImageCount
        FROM dbo.PIC_CAT_T c
        INNER JOIN dbo.PIC_FILES_T p ON p.Cat_ID = c.cat_id AND p.DISPLAY = 1
        GROUP BY c.cat_id, c.name
        HAVING COUNT(p.PIC_ID) > 0
    ) cats;
    INSERT @Results VALUES (
        @Run, 'photos-categories-with-counts', 'all non-empty cats', @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* Largest photo category page 1 */
    DECLARE @LargestCat int = (
        SELECT TOP (1) Cat_ID
        FROM dbo.PIC_FILES_T
        WHERE DISPLAY = 1
        GROUP BY Cat_ID
        ORDER BY COUNT(*) DESC
    );
    SET @StartedAt = SYSUTCDATETIME();
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT p.PIC_ID
        FROM dbo.PIC_FILES_T p
        WHERE p.Cat_ID = @LargestCat AND p.DISPLAY = 1
        ORDER BY p.Date_time DESC, p.PIC_ID DESC
        OFFSET 0 ROWS FETCH NEXT 24 ROWS ONLY
    ) page;
    INSERT @Results VALUES (
        @Run, 'photos-largest-cat-page1', CONCAT('cat=', @LargestCat), @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* Fan performances count + page */
    SET @StartedAt = SYSUTCDATETIME();
    SELECT @Cnt = COUNT(*) FROM dbo.Q_STAGE_T WHERE DISPLAY = 1;
    INSERT @Results VALUES (
        @Run, 'fan-perf-count', 'display=1', 1,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()),
        CONCAT('count=', @Cnt));

    SET @StartedAt = SYSUTCDATETIME();
    SELECT @Rows = COUNT(*)
    FROM (
        SELECT TOP (20) Q_STAGE_ID
        FROM dbo.Q_STAGE_T
        WHERE DISPLAY = 1
        ORDER BY DATE_ADDED DESC
    ) page;
    INSERT @Results VALUES (
        @Run, 'fan-perf-page20', 'top 20', @Rows,
        DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), NULL);

    /* Biography / discography procs (keep; not migration candidates) */
    BEGIN TRY
        SET @StartedAt = SYSUTCDATETIME();
        EXEC dbo.Q_BIO_LIST_SP;
        INSERT @Results VALUES (
            @Run, 'bio-list-sp', 'Q_BIO_LIST_SP', 0,
            DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), 'hot-path proc keep');
    END TRY
    BEGIN CATCH
        INSERT @Results VALUES (
            @Run, 'bio-list-sp', 'Q_BIO_LIST_SP', -1, -1, ERROR_MESSAGE());
    END CATCH;

    BEGIN TRY
        SET @StartedAt = SYSUTCDATETIME();
        EXEC dbo.Q_ALBUM_LIST_SP;
        INSERT @Results VALUES (
            @Run, 'album-list-sp', 'Q_ALBUM_LIST_SP', 0,
            DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()), 'hot-path proc keep');
    END TRY
    BEGIN CATCH
        INSERT @Results VALUES (
            @Run, 'album-list-sp', 'Q_ALBUM_LIST_SP', -1, -1, ERROR_MESSAGE());
    END CATCH;

    SET @Run += 1;
END;

SELECT
    Area,
    Sample,
    COUNT(*) AS Runs,
    AVG(ElapsedMs) AS AvgMs,
    MIN(ElapsedMs) AS MinMs,
    MAX(ElapsedMs) AS MaxMs,
    MAX(RowsRead) AS RowsRead,
    MAX(Notes) AS Notes
FROM @Results
WHERE RunNumber > 0
GROUP BY Area, Sample
ORDER BY AvgMs DESC, Area;

SELECT Area, Sample, RowsRead, ElapsedMs, Notes
FROM @Results
WHERE RunNumber = 0
ORDER BY Area;
