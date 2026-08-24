-- Source of truth for dbo.SearchDocument_Search (unified whole-site search).
-- Applied by EF migrations: 20260804113500_AddSearchDocumentFullTextSearch
-- (proc body) and 20260824120000_AddSearchDocumentSearchSourceKey (SourceKey column).
-- See docs/sql/README.md for contributor conventions.
--
-- Unlike the per-content-type NEWS_T_SearchPublished / ModernForum_SearchThreads procs, this
-- queries one shared table (SearchDocument) already restricted to visible/published rows at
-- index time, so results across all content types share one RANK scale and can be globally
-- ordered and paginated together.

CREATE OR ALTER PROCEDURE dbo.SearchDocument_Search
    @Query        NVARCHAR(500),
    @ContentType  NVARCHAR(50) = NULL,
    @Offset       INT,
    @PageSize     INT,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FtsMatches AS (
        SELECT ft.[KEY] AS DocumentId, ft.[RANK] AS SearchRank
        FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query) ft
    )
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
    INNER JOIN FtsMatches fm ON fm.DocumentId = d.Id
    WHERE  @ContentType IS NULL OR d.ContentType = @ContentType
    ORDER BY fm.SearchRank DESC, d.PublishedAt DESC, d.Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Count in a separate statement so @TotalRecords is set after the result set.
    ;WITH FtsMatchIds AS (
        SELECT ft.[KEY] AS DocumentId
        FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query) ft
    )
    SELECT @TotalRecords = COUNT(*)
    FROM   dbo.SearchDocument d
    INNER JOIN FtsMatchIds fm ON fm.DocumentId = d.Id
    WHERE  @ContentType IS NULL OR d.ContentType = @ContentType;
END;
