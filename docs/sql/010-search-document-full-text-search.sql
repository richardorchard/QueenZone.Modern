-- Source of truth for dbo.SearchDocument_Search (unified whole-site search).
-- Applied by EF migrations: 20260804113500_AddSearchDocumentFullTextSearch
-- (proc body), 20260824120000_AddSearchDocumentSearchSourceKey (SourceKey column),
-- 20260827143000_CapSearchDocumentSearchMatches (single FTS pass + rank cap),
-- and 20260908140000_CapTypedSearchAfterContentTypeFilter (typed search caps after
-- the ContentType filter so a track-title hit is not crowded out of the global top 1000).
-- See docs/sql/README.md for contributor conventions.
--
-- Unlike the per-content-type NEWS_T_SearchPublished / ModernForum_SearchThreads procs, this
-- queries one shared table (SearchDocument) already restricted to visible/published rows at
-- index time, so results across all content types share one RANK scale and can be globally
-- ordered and paginated together.
--
-- Untyped All keeps FREETEXTTABLE top_n_by_rank as a timeout guard: an uncapped double scan
-- of a Queen-heavy archive (every thread mentioning "queen" / "freddie") exceeded the
-- 30-second command timeout and 500'd both /search and GET /api/v1/search.
-- Typed search (@ContentType set) must not reuse that global cap. News and forum rows fill
-- the top 1000 for common terms, so a discography album whose track title lives only in Body
-- never entered #Matches. Filter ContentType first, then apply the same rank cap.

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

    IF @ContentType IS NULL
    BEGIN
        INSERT INTO #Matches (DocumentId, SearchRank)
        SELECT ft.[KEY], ft.[RANK]
        FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query, @MatchLimit) ft;
    END
    ELSE
    BEGIN
        INSERT INTO #Matches (DocumentId, SearchRank)
        SELECT TOP (@MatchLimit)
               d.Id,
               ft.[RANK]
        FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query) ft
        INNER JOIN dbo.SearchDocument d ON d.Id = ft.[KEY]
        WHERE  d.ContentType = @ContentType
        ORDER BY ft.[RANK] DESC, d.PublishedAt DESC, d.Id DESC;
    END

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
