/*
  QueenZone forum full-text search

  Purpose
  -------
  Adds full-text catalog, full-text indexes, and the ModernForum_SearchThreads
  stored procedure that powers the /search page.

  Prerequisites
  -------------
  - 004-modern-forum-batched-import.sql (creates ModernForumThread, ModernForumPost)
  - 006-modern-forum-read-path.sql (creates supporting indexes)
  - Full-text search must be installed on the SQL Server instance

  This script is safe to rerun. FTS catalog and index creation are guarded by
  IF NOT EXISTS checks; CREATE OR ALTER handles the stored procedure.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ModernForumThread', N'U') IS NULL
    THROW 51000, 'dbo.ModernForumThread does not exist. Run 004-modern-forum-batched-import.sql first.', 1;

IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NULL
    THROW 51000, 'dbo.ModernForumPost does not exist. Run 004-modern-forum-batched-import.sql first.', 1;
GO

-- Full-text catalog
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_ForumCatalog')
    CREATE FULLTEXT CATALOG FT_ForumCatalog;
GO

-- Full-text index on thread titles
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumThread'))
    CREATE FULLTEXT INDEX ON dbo.ModernForumThread (Title)
        KEY INDEX PK_ModernForumThread
        ON FT_ForumCatalog
        WITH CHANGE_TRACKING AUTO;
GO

-- Full-text index on post bodies (HTML content; FTS ignores markup tokens)
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost'))
    CREATE FULLTEXT INDEX ON dbo.ModernForumPost (Body)
        KEY INDEX PK_ModernForumPost
        ON FT_ForumCatalog
        WITH CHANGE_TRACKING AUTO;
GO

/*
  ModernForum_SearchThreads
  -------------------------
  Returns a paginated list of forum threads matching the query via full-text
  search on thread titles and post bodies. Only threads that pass the same
  visibility filters as ModernForum_GetCategoryThreadsPage are included
  (IsLegacyTopicStarter = 1, StartedByUserValidated = 1, IsSynthetic = 0).

  Results are ordered by combined FTS rank (title match weighted equal to body
  match) descending, then by LastActivityAt descending.

  Parameters
    @Query        FREETEXT search expression (plain language, no FTS syntax required)
    @Offset       Zero-based row offset for pagination
    @PageSize     Number of rows to return
    @TotalRecords OUTPUT: total matching thread count (for pagination math)
*/
CREATE OR ALTER PROCEDURE dbo.ModernForum_SearchThreads
    @Query        NVARCHAR(500),
    @Offset       INT,
    @PageSize     INT,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Resolve internal PKs for matching thread and post rows, then join back to
    -- thread/category for the public-facing columns (LegacyTopicId, LegacyForumId).
    ;WITH TitleMatches AS
    (
        SELECT t.Id AS ThreadPk, ft.[RANK] AS SearchRank
        FROM dbo.ModernForumThread t
        INNER JOIN FREETEXTTABLE(dbo.ModernForumThread, Title, @Query) ft ON ft.[KEY] = t.Id
        INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
        WHERE t.IsLegacyTopicStarter = 1
          AND t.StartedByUserValidated = 1
          AND c.IsSynthetic = 0
    ),
    BodyMatches AS
    (
        SELECT p.ThreadId AS ThreadPk, MAX(ft.[RANK]) AS SearchRank
        FROM dbo.ModernForumPost p
        INNER JOIN FREETEXTTABLE(dbo.ModernForumPost, Body, @Query) ft ON ft.[KEY] = p.Id
        INNER JOIN dbo.ModernForumThread t ON t.Id = p.ThreadId
        INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
        WHERE t.IsLegacyTopicStarter = 1
          AND t.StartedByUserValidated = 1
          AND c.IsSynthetic = 0
        GROUP BY p.ThreadId
    ),
    Combined AS
    (
        SELECT
            COALESCE(tm.ThreadPk, bm.ThreadPk) AS ThreadPk,
            COALESCE(tm.SearchRank, 0) + COALESCE(bm.SearchRank, 0) AS TotalRank
        FROM TitleMatches tm
        FULL OUTER JOIN BodyMatches bm ON bm.ThreadPk = tm.ThreadPk
    )
    SELECT
        t.LegacyTopicId  AS TopicId,
        LTRIM(RTRIM(t.Title)) AS Title,
        c.LegacyForumId  AS CategoryId,
        c.Name           AS CategoryName,
        ISNULL(t.ReplyCount, 0) AS ReplyCount,
        t.LastActivityAt,
        t.StartedByDisplayName
    FROM Combined r
    INNER JOIN dbo.ModernForumThread t ON t.Id = r.ThreadPk
    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
    ORDER BY r.TotalRank DESC, t.LastActivityAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Total count for pagination (separate query avoids double-materialising FTS results)
    ;WITH TitleMatchIds AS
    (
        SELECT ft.[KEY] AS ThreadPk
        FROM FREETEXTTABLE(dbo.ModernForumThread, Title, @Query) ft
        INNER JOIN dbo.ModernForumThread t ON t.Id = ft.[KEY]
        INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
        WHERE t.IsLegacyTopicStarter = 1
          AND t.StartedByUserValidated = 1
          AND c.IsSynthetic = 0
    ),
    BodyMatchIds AS
    (
        SELECT DISTINCT p.ThreadId AS ThreadPk
        FROM dbo.ModernForumPost p
        INNER JOIN FREETEXTTABLE(dbo.ModernForumPost, Body, @Query) ft ON ft.[KEY] = p.Id
        INNER JOIN dbo.ModernForumThread t ON t.Id = p.ThreadId
        INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
        WHERE t.IsLegacyTopicStarter = 1
          AND t.StartedByUserValidated = 1
          AND c.IsSynthetic = 0
    )
    SELECT @TotalRecords = COUNT(*)
    FROM
    (
        SELECT ThreadPk FROM TitleMatchIds
        UNION
        SELECT ThreadPk FROM BodyMatchIds
    ) u;
END;
GO
