-- Source of truth for dbo.SearchDocument_Search (unified whole-site search).
-- Applied by EF migrations: 20260804113500_AddSearchDocumentFullTextSearch
-- (proc body), 20260824120000_AddSearchDocumentSearchSourceKey (SourceKey column),
-- and 20260827143000_CapSearchDocumentSearchMatches (single FTS pass + rank cap).
-- See docs/sql/README.md for contributor conventions.
--
-- Unlike the per-content-type NEWS_T_SearchPublished / ModernForum_SearchThreads procs, this
-- queries one shared table (SearchDocument) already restricted to visible/published rows at
-- index time, so results across all content types share one RANK scale and can be globally
-- ordered and paginated together.
--
-- FREETEXTTABLE is invoked once and capped with top_n_by_rank. An uncapped double scan of a
-- Queen-heavy archive (every thread mentioning "queen" / "freddie") exceeded the 30-second
-- command timeout and 500'd both /search and GET /api/v1/search.

CREATE OR ALTER PROCEDURE dbo.SearchDocument_Search
    @Query        NVARCHAR(500),
    @ContentType  NVARCHAR(50) = NULL,
    @Offset       INT,
    @PageSize     INT,
    @TotalRecords INT OUTPUT,
    @RankLimit    INT = 1000
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MatchLimit INT = CASE
        WHEN @RankLimit IS NULL OR @RankLimit < 1 THEN 1000
        WHEN @RankLimit > 1000 THEN 1000
        ELSE @RankLimit
    END;

    CREATE TABLE #Matches
    (
        DocumentId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        SearchRank INT NOT NULL
    );

    INSERT INTO #Matches (DocumentId, SearchRank)
    SELECT ft.[KEY], ft.[RANK]
    FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query, @MatchLimit) ft;

    SELECT
        d.ContentType,
        d.SourceKey,
        d.Title,
        d.Summary,
        d.Url,
        d.PublishedAt,
        d.ImageUrl,
        d.Category,
        d.AuthorDisplayName
    FROM   dbo.SearchDocument d
    INNER JOIN #Matches fm ON fm.DocumentId = d.Id
    WHERE  @ContentType IS NULL OR d.ContentType = @ContentType
    ORDER BY fm.SearchRank DESC, d.PublishedAt DESC, d.Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalRecords = COUNT(*)
    FROM   dbo.SearchDocument d
    INNER JOIN #Matches fm ON fm.DocumentId = d.Id
    WHERE  @ContentType IS NULL OR d.ContentType = @ContentType;
END;
