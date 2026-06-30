/*
  QueenZone forum archive read performance benchmark

  Purpose
  -------
  Compares the current public forum read path against equivalent reads from
  the populated ModernForum* tables.

  This script is read-only. It does not create or modify database objects.

  Suggested sqlcmd usage:

      sqlcmd -S <server> -d <database> -U <user> -P <password> -C -b -I -t 0 -i docs/sql/005-forum-read-performance-benchmark.sql

  Notes
  -----
  - Legacy category pages use dbo.Q_FORUM_VIEW_PAGE_SP. That procedure
    filters displayed rows to validated users but counts all topic_starter = 1
    rows in @TotalRecords; the modern comparison mirrors that behavior.
  - Legacy topic pages use dbo.Q_FORUM_TOPIC_NEW_SP.
  - Current legacy sitemap code only counts Q_FORUM_TOPIC_PARENT_ID = 0 rows;
    the corrected modern import has a wider thread definition, so the
    sitemap count comparison is expected to show a semantic mismatch.
  - Run more than once when comparing cold and warm cache behavior.
*/

SET NOCOUNT ON;

DECLARE @Runs int = 3;
DECLARE @ForumId int = 10;
DECLARE @TopicId int = 1495269;
DECLARE @TopicsPage int = 1;
DECLARE @DeepTopicsPage int = 100;
DECLARE @TopicPostsPage int = 1;
DECLARE @DeepTopicPostsPage int = 100;
DECLARE @TopicsPageSize int = 25;
DECLARE @PostsPageSize int = 15;
DECLARE @SitemapPageSize int = 50000;

DECLARE @Results table
(
    RunNumber int NOT NULL,
    Area varchar(40) NOT NULL,
    Source varchar(12) NOT NULL,
    Sample varchar(80) NOT NULL,
    RowsRead int NOT NULL,
    TotalRows bigint NULL,
    ElapsedMs int NOT NULL
);

DECLARE @Run int = 1;

WHILE @Run <= @Runs
BEGIN
    DECLARE @StartedAt datetime2(7);
    DECLARE @RowsRead int;
    DECLARE @TotalRows bigint;
    DECLARE @TotalRecords int;
    DECLARE @Subscribed int;
    DECLARE @ForumName varchar(30);
    DECLARE @Subject varchar(75);
    DECLARE @OutputForumId int;
    DECLARE @Disco tinyint;

    DECLARE @LegacyCategories table
    (
        Id int NULL,
        Name nvarchar(100) NULL,
        Description nvarchar(400) NULL,
        PostCount int NULL,
        LastActivityAt datetime NULL,
        LatestThreadTitle nvarchar(200) NULL,
        SortOrder int NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    INSERT @LegacyCategories
    SELECT
        CAST(f.Q_FORUM_ID AS int) AS Id,
        f.Q_FORUM_NAME AS Name,
        NULLIF(LTRIM(RTRIM(f.Q_FORUM_DESCRIPTION)), '') AS Description,
        ISNULL(f.Q_FORUM_POST_COUNT, 0) AS PostCount,
        f.Q_FORUM_LAST_POST AS LastActivityAt,
        CAST(NULL AS nvarchar(200)) AS LatestThreadTitle,
        ISNULL(CAST(f.FORUM_ORDER AS int), 0) AS SortOrder
    FROM dbo.Q_FORUM_T f
    ORDER BY f.FORUM_ORDER ASC, f.Q_FORUM_ID ASC;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'categories', 'legacy', 'forum index', @RowsRead, @RowsRead, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @ModernCategories table
    (
        Id int NULL,
        Name nvarchar(100) NULL,
        Description nvarchar(400) NULL,
        PostCount int NULL,
        LastActivityAt datetime2(0) NULL,
        LatestThreadTitle nvarchar(200) NULL,
        SortOrder int NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    INSERT @ModernCategories
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

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'categories', 'modern', 'forum index', @RowsRead, @RowsRead, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @LegacyCategoryById table
    (
        Id int NULL,
        Name nvarchar(100) NULL,
        Description nvarchar(400) NULL,
        PostCount int NULL,
        LastActivityAt datetime NULL,
        LatestThreadTitle nvarchar(200) NULL,
        SortOrder int NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    INSERT @LegacyCategoryById
    SELECT
        CAST(f.Q_FORUM_ID AS int) AS Id,
        f.Q_FORUM_NAME AS Name,
        NULLIF(LTRIM(RTRIM(f.Q_FORUM_DESCRIPTION)), '') AS Description,
        ISNULL(f.Q_FORUM_POST_COUNT, 0) AS PostCount,
        f.Q_FORUM_LAST_POST AS LastActivityAt,
        NULLIF(LTRIM(RTRIM(latest.TOPIC_SUBJECT)), '') AS LatestThreadTitle,
        ISNULL(CAST(f.FORUM_ORDER AS int), 0) AS SortOrder
    FROM dbo.Q_FORUM_T f
    OUTER APPLY (
        SELECT TOP (1) t.TOPIC_SUBJECT
        FROM dbo.Q_FORUM_TOPIC_T t
        WHERE t.Q_FORUM_ID = f.Q_FORUM_ID
          AND t.TOPIC_STARTER = 1
        ORDER BY t.TOPIC_LAST_POST DESC
    ) latest
    WHERE f.Q_FORUM_ID = @ForumId;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'category detail', 'legacy', CONCAT('forum ', @ForumId), @RowsRead, @RowsRead, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @ModernCategoryById table
    (
        Id int NULL,
        Name nvarchar(100) NULL,
        Description nvarchar(400) NULL,
        PostCount int NULL,
        LastActivityAt datetime2(0) NULL,
        LatestThreadTitle nvarchar(200) NULL,
        SortOrder int NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    INSERT @ModernCategoryById
    SELECT
        c.LegacyForumId AS Id,
        c.Name,
        NULLIF(LTRIM(RTRIM(c.Description)), '') AS Description,
        c.LegacyPostCount AS PostCount,
        c.LastActivityAt,
        latest.Title AS LatestThreadTitle,
        c.SortOrder
    FROM dbo.ModernForumCategory c
    OUTER APPLY (
        SELECT TOP (1) t.Title
        FROM dbo.ModernForumThread t
        WHERE t.CategoryId = c.Id
          AND t.IsLegacyTopicStarter = 1
        ORDER BY t.LastActivityAt DESC, t.LegacyTopicId DESC
    ) latest
    WHERE c.LegacyForumId = @ForumId
      AND c.IsSynthetic = 0;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'category detail', 'modern', CONCAT('forum ', @ForumId), @RowsRead, @RowsRead, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @LegacyTopics table
    (
        Id int NOT NULL,
        Q_FORUM_TOPIC_ID int NULL,
        TOPIC_SUBJECT varchar(150) NULL,
        TOPIC_LAST_POST smalldatetime NULL,
        USER_ID int NULL,
        USERNAME varchar(100) NULL,
        NUMBEROFREPLIES smallint NULL,
        LAST_POST_USERNAME varchar(100) NULL,
        STICKY tinyint NULL
    );

    SET @TotalRecords = 0;
    SET @StartedAt = SYSUTCDATETIME();

    INSERT @LegacyTopics
    EXEC dbo.Q_FORUM_VIEW_PAGE_SP
        @CurrentPage = @TopicsPage,
        @PageSize = @TopicsPageSize,
        @Q_FORUM_ID = @ForumId,
        @TotalRecords = @TotalRecords OUTPUT;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'category topics', 'legacy', CONCAT('forum ', @ForumId, ' page ', @TopicsPage), @RowsRead, @TotalRecords, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @ModernTopics table
    (
        Id int NOT NULL,
        Q_FORUM_TOPIC_ID int NULL,
        TOPIC_SUBJECT nvarchar(200) NULL,
        TOPIC_LAST_POST datetime2(0) NULL,
        USER_ID int NULL,
        USERNAME nvarchar(100) NULL,
        NUMBEROFREPLIES int NULL,
        LAST_POST_USERNAME nvarchar(100) NULL,
        STICKY tinyint NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    INSERT @ModernTopics
    SELECT
        CAST(0 AS int) AS Id,
        t.LegacyTopicId AS Q_FORUM_TOPIC_ID,
        t.Title AS TOPIC_SUBJECT,
        t.LastActivityAt AS TOPIC_LAST_POST,
        t.StartedByLegacyUserId AS USER_ID,
        t.StartedByDisplayName AS USERNAME,
        t.ReplyCount AS NUMBEROFREPLIES,
        CAST(NULL AS nvarchar(100)) AS LAST_POST_USERNAME,
        CAST(CASE WHEN t.IsSticky = 1 THEN 1 ELSE 0 END AS tinyint) AS STICKY
    FROM dbo.ModernForumThread t
    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
    WHERE c.LegacyForumId = @ForumId
      AND c.IsSynthetic = 0
      AND t.IsLegacyTopicStarter = 1
      AND t.StartedByUserValidated = 1
    ORDER BY t.IsSticky DESC, t.LastActivityAt DESC, t.LegacyTopicId ASC
    OFFSET ((@TopicsPage - 1) * @TopicsPageSize) ROWS FETCH NEXT @TopicsPageSize ROWS ONLY;

    SET @RowsRead = @@ROWCOUNT;

    SELECT @TotalRows = s.TotalThreads
    FROM dbo.ModernForumCategory c
    INNER JOIN dbo.ModernForumCategoryReadStats s ON s.CategoryId = c.Id
    WHERE c.LegacyForumId = @ForumId
      AND c.IsSynthetic = 0;

    INSERT @Results
    VALUES (@Run, 'category topics', 'modern', CONCAT('forum ', @ForumId, ' page ', @TopicsPage), @RowsRead, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DELETE FROM @LegacyTopics;
    SET @TotalRecords = 0;
    SET @StartedAt = SYSUTCDATETIME();

    INSERT @LegacyTopics
    EXEC dbo.Q_FORUM_VIEW_PAGE_SP
        @CurrentPage = @DeepTopicsPage,
        @PageSize = @TopicsPageSize,
        @Q_FORUM_ID = @ForumId,
        @TotalRecords = @TotalRecords OUTPUT;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'category topics', 'legacy', CONCAT('forum ', @ForumId, ' page ', @DeepTopicsPage), @RowsRead, @TotalRecords, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DELETE FROM @ModernTopics;
    SET @StartedAt = SYSUTCDATETIME();

    INSERT @ModernTopics
    SELECT
        CAST(0 AS int) AS Id,
        t.LegacyTopicId AS Q_FORUM_TOPIC_ID,
        t.Title AS TOPIC_SUBJECT,
        t.LastActivityAt AS TOPIC_LAST_POST,
        t.StartedByLegacyUserId AS USER_ID,
        t.StartedByDisplayName AS USERNAME,
        t.ReplyCount AS NUMBEROFREPLIES,
        CAST(NULL AS nvarchar(100)) AS LAST_POST_USERNAME,
        CAST(CASE WHEN t.IsSticky = 1 THEN 1 ELSE 0 END AS tinyint) AS STICKY
    FROM dbo.ModernForumThread t
    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
    WHERE c.LegacyForumId = @ForumId
      AND c.IsSynthetic = 0
      AND t.IsLegacyTopicStarter = 1
      AND t.StartedByUserValidated = 1
    ORDER BY t.IsSticky DESC, t.LastActivityAt DESC, t.LegacyTopicId ASC
    OFFSET ((@DeepTopicsPage - 1) * @TopicsPageSize) ROWS FETCH NEXT @TopicsPageSize ROWS ONLY;

    SET @RowsRead = @@ROWCOUNT;

    SELECT @TotalRows = s.TotalThreads
    FROM dbo.ModernForumCategory c
    INNER JOIN dbo.ModernForumCategoryReadStats s ON s.CategoryId = c.Id
    WHERE c.LegacyForumId = @ForumId
      AND c.IsSynthetic = 0;

    INSERT @Results
    VALUES (@Run, 'category topics', 'modern', CONCAT('forum ', @ForumId, ' page ', @DeepTopicsPage), @RowsRead, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @LegacyPosts table
    (
        TOPIC_MESSAGE varchar(8000) NOT NULL,
        TOPIC_DATE smalldatetime NULL,
        USER_ID int NULL,
        USERNAME char(40) NULL,
        SIGNATURE varchar(200) NULL,
        NUMBER_OF_POSTS smallint NULL,
        DATE_CREATED smalldatetime NULL,
        Q_FORUM_TOPIC_ID int NULL,
        ATTACHMENT varchar(120) NULL,
        FILESIZE char(12) NULL,
        ATTACH_COUNT smallint NULL,
        ONLINE tinyint NULL,
        AVATAR varchar(50) NULL,
        DISPLAY_MESSAGE varchar(30) NULL,
        DISCO tinyint NULL
    );

    SET @TotalRecords = 0;
    SET @Subscribed = 0;
    SET @ForumName = '';
    SET @Subject = '';
    SET @OutputForumId = 0;
    SET @Disco = 0;
    SET @StartedAt = SYSUTCDATETIME();

    INSERT @LegacyPosts
    EXEC dbo.Q_FORUM_TOPIC_NEW_SP
        @CurrentPage = @TopicPostsPage,
        @PageSize = @PostsPageSize,
        @Q_FORUM_TOPIC_ID = @TopicId,
        @USER_ID = 0,
        @TotalRecords = @TotalRecords OUTPUT,
        @SUBSCRIBED = @Subscribed OUTPUT,
        @forum_name = @ForumName OUTPUT,
        @SUBJECT = @Subject OUTPUT,
        @Q_FORUM_ID = @OutputForumId OUTPUT,
        @DISCO = @Disco OUTPUT;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'topic posts', 'legacy', CONCAT('topic ', @TopicId, ' page ', @TopicPostsPage), @RowsRead, @TotalRecords, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @ModernPosts table
    (
        TOPIC_MESSAGE varchar(8000) NOT NULL,
        TOPIC_DATE datetime2(0) NULL,
        USER_ID int NULL,
        USERNAME nvarchar(100) NULL,
        SIGNATURE varchar(8000) NULL,
        NUMBER_OF_POSTS int NULL,
        DATE_CREATED datetime2(0) NULL,
        Q_FORUM_TOPIC_ID int NULL,
        ATTACHMENT varchar(120) NULL,
        FILESIZE varchar(12) NULL,
        ATTACH_COUNT int NULL,
        ONLINE tinyint NULL,
        AVATAR varchar(50) NULL,
        DISPLAY_MESSAGE varchar(30) NULL,
        DISCO tinyint NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    INSERT @ModernPosts
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
    FROM dbo.ModernForumPost p
    INNER JOIN dbo.ModernForumThread t ON t.Id = p.ThreadId
    WHERE t.LegacyTopicId = @TopicId
    ORDER BY p.LegacyPostId ASC
    OFFSET ((@TopicPostsPage - 1) * @PostsPageSize) ROWS FETCH NEXT @PostsPageSize ROWS ONLY;

    SET @RowsRead = @@ROWCOUNT;

    SELECT @TotalRows = s.PostCount
    FROM dbo.ModernForumThreadReadStats s
    WHERE s.LegacyTopicId = @TopicId;

    INSERT @Results
    VALUES (@Run, 'topic posts', 'modern', CONCAT('topic ', @TopicId, ' page ', @TopicPostsPage), @RowsRead, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DELETE FROM @LegacyPosts;
    SET @TotalRecords = 0;
    SET @Subscribed = 0;
    SET @ForumName = '';
    SET @Subject = '';
    SET @OutputForumId = 0;
    SET @Disco = 0;
    SET @StartedAt = SYSUTCDATETIME();

    INSERT @LegacyPosts
    EXEC dbo.Q_FORUM_TOPIC_NEW_SP
        @CurrentPage = @DeepTopicPostsPage,
        @PageSize = @PostsPageSize,
        @Q_FORUM_TOPIC_ID = @TopicId,
        @USER_ID = 0,
        @TotalRecords = @TotalRecords OUTPUT,
        @SUBSCRIBED = @Subscribed OUTPUT,
        @forum_name = @ForumName OUTPUT,
        @SUBJECT = @Subject OUTPUT,
        @Q_FORUM_ID = @OutputForumId OUTPUT,
        @DISCO = @Disco OUTPUT;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'topic posts', 'legacy', CONCAT('topic ', @TopicId, ' page ', @DeepTopicPostsPage), @RowsRead, @TotalRecords, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DELETE FROM @ModernPosts;
    SET @StartedAt = SYSUTCDATETIME();

    INSERT @ModernPosts
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
    FROM dbo.ModernForumPost p
    INNER JOIN dbo.ModernForumThread t ON t.Id = p.ThreadId
    WHERE t.LegacyTopicId = @TopicId
    ORDER BY p.LegacyPostId ASC
    OFFSET ((@DeepTopicPostsPage - 1) * @PostsPageSize) ROWS FETCH NEXT @PostsPageSize ROWS ONLY;

    SET @RowsRead = @@ROWCOUNT;

    SELECT @TotalRows = s.PostCount
    FROM dbo.ModernForumThreadReadStats s
    WHERE s.LegacyTopicId = @TopicId;

    INSERT @Results
    VALUES (@Run, 'topic posts', 'modern', CONCAT('topic ', @TopicId, ' page ', @DeepTopicPostsPage), @RowsRead, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    SET @StartedAt = SYSUTCDATETIME();

    SELECT @TotalRows = ISNULL(SUM(CAST(THREADCOUNT AS bigint)), 0)
    FROM dbUser.Q_FORUM_TOPIC_THREAD_COUNT_V;

    INSERT @Results
    VALUES (@Run, 'thread count', 'legacy', 'archive stats', 1, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    SET @StartedAt = SYSUTCDATETIME();

    SELECT @TotalRows = TotalThreads
    FROM dbo.ModernForumArchiveReadStats
    WHERE Id = 1;

    INSERT @Results
    VALUES (@Run, 'thread count', 'modern', 'archive stats', 1, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @LegacySitemap table
    (
        TopicId int NOT NULL,
        Title varchar(150) NULL,
        LastActivityAt smalldatetime NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    SELECT @TotalRows = COUNT_BIG(*)
    FROM dbo.Q_FORUM_TOPIC_T
    WHERE Q_FORUM_TOPIC_PARENT_ID = 0
      AND LTRIM(RTRIM(ISNULL(TOPIC_SUBJECT, ''))) <> '';

    INSERT @LegacySitemap
    SELECT
        t.Q_FORUM_TOPIC_ID AS TopicId,
        LTRIM(RTRIM(t.TOPIC_SUBJECT)) AS Title,
        t.TOPIC_LAST_POST AS LastActivityAt
    FROM dbo.Q_FORUM_TOPIC_T t
    WHERE t.Q_FORUM_TOPIC_PARENT_ID = 0
      AND LTRIM(RTRIM(ISNULL(t.TOPIC_SUBJECT, ''))) <> ''
    ORDER BY t.Q_FORUM_TOPIC_ID ASC
    OFFSET 0 ROWS FETCH NEXT @SitemapPageSize ROWS ONLY;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'sitemap', 'legacy', 'file 1', @RowsRead, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    DECLARE @ModernSitemap table
    (
        TopicId int NOT NULL,
        Title nvarchar(200) NULL,
        LastActivityAt datetime2(0) NULL
    );

    SET @StartedAt = SYSUTCDATETIME();

    SELECT @TotalRows = SitemapTopicCount
    FROM dbo.ModernForumArchiveReadStats
    WHERE Id = 1;

    INSERT @ModernSitemap
    SELECT
        t.LegacyTopicId AS TopicId,
        LTRIM(RTRIM(t.Title)) AS Title,
        t.LastActivityAt
    FROM dbo.ModernForumThread t
    WHERE NULLIF(LTRIM(RTRIM(t.Title)), '') IS NOT NULL
    ORDER BY t.LegacyTopicId ASC
    OFFSET 0 ROWS FETCH NEXT @SitemapPageSize ROWS ONLY;

    SET @RowsRead = @@ROWCOUNT;

    INSERT @Results
    VALUES (@Run, 'sitemap', 'modern', 'file 1', @RowsRead, @TotalRows, DATEDIFF(millisecond, @StartedAt, SYSUTCDATETIME()));

    SET @Run += 1;
END;

SELECT
    RunNumber,
    Area,
    Source,
    Sample,
    RowsRead,
    TotalRows,
    ElapsedMs
FROM @Results
ORDER BY
    RunNumber,
    CASE Area
        WHEN 'categories' THEN 1
        WHEN 'category detail' THEN 2
        WHEN 'category topics' THEN 3
        WHEN 'topic posts' THEN 4
        WHEN 'thread count' THEN 5
        WHEN 'sitemap' THEN 6
        ELSE 99
    END,
    Sample,
    Source;

SELECT
    Area,
    Source,
    Sample,
    COUNT(*) AS Runs,
    MIN(ElapsedMs) AS MinElapsedMs,
    AVG(CAST(ElapsedMs AS decimal(18, 2))) AS AvgElapsedMs,
    MAX(ElapsedMs) AS MaxElapsedMs,
    MIN(RowsRead) AS MinRowsRead,
    MAX(RowsRead) AS MaxRowsRead,
    MIN(TotalRows) AS MinTotalRows,
    MAX(TotalRows) AS MaxTotalRows
FROM @Results
GROUP BY Area, Source, Sample
ORDER BY
    CASE Area
        WHEN 'categories' THEN 1
        WHEN 'category detail' THEN 2
        WHEN 'category topics' THEN 3
        WHEN 'topic posts' THEN 4
        WHEN 'thread count' THEN 5
        WHEN 'sitemap' THEN 6
        ELSE 99
    END,
    Sample,
    Source;
