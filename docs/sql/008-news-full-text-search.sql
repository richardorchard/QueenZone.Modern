-- Source of truth for dbo.NEWS_T_SearchPublished (public news archive FTS).
-- Applied by EF migration: 20260729000000_AddNewsFullTextSearch
-- See docs/sql/README.md for contributor conventions.

CREATE OR ALTER PROCEDURE dbo.NEWS_T_SearchPublished
    @Query        NVARCHAR(500),
    @Offset       INT,
    @PageSize     INT,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Ranked FTS matches joined to deduplicated published rows.
    -- ROW_NUMBER guards against rare legacy duplicate NEWS_ID values.
    ;WITH FtsMatches AS (
        SELECT ft.[KEY] AS NewsId, ft.[RANK] AS SearchRank
        FROM   FREETEXTTABLE(dbo.NEWS_T, (TITLE, EXCERPT, ARTICLE), @Query) ft
    ),
    LatestPublished AS (
        SELECT
            n.NEWS_ID               AS Id,
            ISNULL(n.TITLE, '')     AS Title,
            ISNULL(n.EXCERPT, '')   AS Excerpt,
            n.[DATE]                AS PublishedAt,
            n.SOURCE_URL            AS SourceUrl,
            CAST(1 AS bit)          AS IsPublished,
            n.SLUG                  AS Slug,
            fm.SearchRank,
            ROW_NUMBER() OVER (PARTITION BY n.NEWS_ID ORDER BY n.[DATE] DESC, n.NEWS_ID DESC) AS RowNum
        FROM   dbo.NEWS_T n
        INNER JOIN FtsMatches fm ON fm.NewsId = n.NEWS_ID
        WHERE  n.DISPLAY = 1
    )
    SELECT
        Id,
        Title,
        Excerpt,
        CAST(N'' AS nvarchar(max)) AS Body,
        PublishedAt,
        SourceUrl,
        IsPublished,
        Slug
    FROM LatestPublished
    WHERE RowNum = 1
    ORDER BY SearchRank DESC, PublishedAt DESC, Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Count in a separate statement so @TotalRecords is set after the result set.
    ;WITH FtsMatchIds AS (
        SELECT ft.[KEY] AS NewsId
        FROM   FREETEXTTABLE(dbo.NEWS_T, (TITLE, EXCERPT, ARTICLE), @Query) ft
    ),
    LatestPublishedIds AS (
        SELECT
            n.NEWS_ID,
            ROW_NUMBER() OVER (PARTITION BY n.NEWS_ID ORDER BY n.[DATE] DESC, n.NEWS_ID DESC) AS RowNum
        FROM dbo.NEWS_T n
        INNER JOIN FtsMatchIds fm ON fm.NewsId = n.NEWS_ID
        WHERE  n.DISPLAY = 1
    )
    SELECT @TotalRecords = COUNT(*)
    FROM LatestPublishedIds
    WHERE RowNum = 1;
END;
