/*
  QueenZone modern forum public read path

  Purpose
  -------
  Adds read-only stored procedures and covering indexes for serving the public
  forum archive from dbo.ModernForumCategory, dbo.ModernForumThread, and
  dbo.ModernForumPost.

  This script is safe to rerun. It creates indexes only when they are missing
  and uses CREATE OR ALTER for stored procedures.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ModernForumCategory', N'U') IS NULL
    THROW 51000, 'dbo.ModernForumCategory does not exist. Run 004-modern-forum-batched-import.sql first.', 1;

IF OBJECT_ID(N'dbo.ModernForumThread', N'U') IS NULL
    THROW 51000, 'dbo.ModernForumThread does not exist. Run 004-modern-forum-batched-import.sql first.', 1;

IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NULL
    THROW 51000, 'dbo.ModernForumPost does not exist. Run 004-modern-forum-batched-import.sql first.', 1;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ModernForumThread', N'U')
      AND name = N'IX_ModernForumThread_CategoryStarter_Latest'
)
BEGIN
    CREATE INDEX IX_ModernForumThread_CategoryStarter_Latest
    ON dbo.ModernForumThread (CategoryId, IsLegacyTopicStarter, LastActivityAt DESC, LegacyTopicId DESC)
    INCLUDE (Title);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ModernForumThread', N'U')
      AND name = N'IX_ModernForumThread_PublicCategoryPage'
)
BEGIN
    CREATE INDEX IX_ModernForumThread_PublicCategoryPage
    ON dbo.ModernForumThread
    (
        CategoryId,
        IsLegacyTopicStarter,
        StartedByUserValidated,
        IsSticky DESC,
        LastActivityAt DESC,
        LegacyTopicId ASC
    )
    INCLUDE (Title, StartedByLegacyUserId, StartedByDisplayName, ReplyCount);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ModernForumThread', N'U')
      AND name = N'IX_ModernForumThread_Sitemap'
)
BEGIN
    CREATE INDEX IX_ModernForumThread_Sitemap
    ON dbo.ModernForumThread (LegacyTopicId ASC)
    INCLUDE (Title, LastActivityAt);
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetCategories
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.LegacyForumId AS Id,
        c.Name,
        NULLIF(LTRIM(RTRIM(c.Description)), '') AS Description,
        c.LegacyPostCount AS PostCount,
        c.LastActivityAt,
        CAST(NULL AS nvarchar(200)) AS LatestThreadTitle,
        c.SortOrder
    FROM dbo.ModernForumCategory c
    WHERE c.IsSynthetic = 0
    ORDER BY c.SortOrder ASC, c.LegacyForumId ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetCategoryByLegacyForumId
    @Q_FORUM_ID int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.LegacyForumId AS Id,
        c.Name,
        NULLIF(LTRIM(RTRIM(c.Description)), '') AS Description,
        c.LegacyPostCount AS PostCount,
        c.LastActivityAt,
        latest.Title AS LatestThreadTitle,
        c.SortOrder
    FROM dbo.ModernForumCategory c
    OUTER APPLY
    (
        SELECT TOP (1) t.Title
        FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_CategoryStarter_Latest))
        WHERE t.CategoryId = c.Id
          AND t.IsLegacyTopicStarter = 1
        ORDER BY t.LastActivityAt DESC, t.LegacyTopicId DESC
    ) latest
    WHERE c.LegacyForumId = @Q_FORUM_ID
      AND c.IsSynthetic = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetCategoryThreadsPage
    @CurrentPage int,
    @PageSize int,
    @Q_FORUM_ID int,
    @TotalRecords int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset int = (CASE WHEN @CurrentPage > 1 THEN @CurrentPage - 1 ELSE 0 END) * @PageSize;

    SELECT @TotalRecords = COUNT_BIG(*)
    FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_PublicCategoryPage))
    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
    WHERE c.LegacyForumId = @Q_FORUM_ID
      AND c.IsSynthetic = 0
      AND t.IsLegacyTopicStarter = 1;

    SELECT
        CAST(ROW_NUMBER() OVER (ORDER BY t.IsSticky DESC, t.LastActivityAt DESC, t.LegacyTopicId ASC) AS int) AS Id,
        t.LegacyTopicId AS Q_FORUM_TOPIC_ID,
        t.Title AS TOPIC_SUBJECT,
        t.LastActivityAt AS TOPIC_LAST_POST,
        t.StartedByLegacyUserId AS USER_ID,
        t.StartedByDisplayName AS USERNAME,
        t.ReplyCount AS NUMBEROFREPLIES,
        CAST(NULL AS nvarchar(100)) AS LAST_POST_USERNAME,
        CAST(CASE WHEN t.IsSticky = 1 THEN 1 ELSE 0 END AS tinyint) AS STICKY
    FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_PublicCategoryPage))
    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
    WHERE c.LegacyForumId = @Q_FORUM_ID
      AND c.IsSynthetic = 0
      AND t.IsLegacyTopicStarter = 1
      AND ISNULL(t.StartedByUserValidated, 0) = 1
    ORDER BY t.IsSticky DESC, t.LastActivityAt DESC, t.LegacyTopicId ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicPostsPage
    @CurrentPage int,
    @PageSize int,
    @Q_FORUM_TOPIC_ID int,
    @TotalRecords int OUTPUT,
    @forum_name nvarchar(100) OUTPUT,
    @SUBJECT nvarchar(200) OUTPUT,
    @Q_FORUM_ID int OUTPUT,
    @DISCO tinyint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset int = (CASE WHEN @CurrentPage > 1 THEN @CurrentPage - 1 ELSE 0 END) * @PageSize;
    DECLARE @ThreadId bigint;

    SELECT
        @ThreadId = t.Id,
        @SUBJECT = t.Title,
        @Q_FORUM_ID = c.LegacyForumId,
        @forum_name = c.Name,
        @DISCO = t.LegacyDiscography
    FROM dbo.ModernForumThread t
    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
    WHERE t.LegacyTopicId = @Q_FORUM_TOPIC_ID;

    IF @ThreadId IS NULL
    BEGIN
        SET @TotalRecords = 0;
        RETURN;
    END;

    SELECT @TotalRecords = COUNT_BIG(*)
    FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
    WHERE p.ThreadId = @ThreadId;

    SELECT
        p.BodyHtml AS TOPIC_MESSAGE,
        p.PostedAt AS TOPIC_DATE,
        p.AuthorLegacyUserId AS USER_ID,
        p.AuthorDisplayName AS USERNAME,
        p.SignatureHtml AS SIGNATURE,
        p.AuthorPostCount AS NUMBER_OF_POSTS,
        p.AuthorJoinedAt AS DATE_CREATED,
        p.LegacyPostId AS Q_FORUM_TOPIC_ID,
        p.Attachment AS ATTACHMENT,
        p.FileSize AS FILESIZE,
        p.AttachCount AS ATTACH_COUNT,
        CAST(0 AS tinyint) AS ONLINE,
        CAST(NULL AS varchar(50)) AS AVATAR,
        CAST(NULL AS varchar(30)) AS DISPLAY_MESSAGE,
        p.LegacyDiscography AS DISCO
    FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
    WHERE p.ThreadId = @ThreadId
    ORDER BY p.LegacyPostId ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTotalThreadCount
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(*)
    FROM dbo.ModernForumThread;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicSitemapCount
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(*)
    FROM dbo.ModernForumThread
    WHERE NULLIF(LTRIM(RTRIM(Title)), '') IS NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicSitemapPage
    @Offset int,
    @PageSize int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.LegacyTopicId AS TopicId,
        LTRIM(RTRIM(t.Title)) AS Title,
        t.LastActivityAt
    FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_Sitemap))
    WHERE NULLIF(LTRIM(RTRIM(t.Title)), '') IS NOT NULL
    ORDER BY t.LegacyTopicId ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
