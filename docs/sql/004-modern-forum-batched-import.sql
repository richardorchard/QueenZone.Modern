/*
QueenZone modern forum archive batched import.

Purpose:
  Create modern read-model tables for the public forum archive and import
  legacy Q_FORUM_* rows in small, resumable batches suitable for the 5 DTU
  Azure SQL tier.

Scope:
  - Creates new dbo.ModernForum* tables and dbo.ImportCheckpoint.
  - Reads legacy forum tables.
  - Does not mutate legacy Q_FORUM_* or USERS_T data.
  - Imports public archive-shaped data only; private fields such as emails,
    passwords, IP addresses, and private messages are not copied.

Recommended 5 DTU run order:
  1. EXEC dbo.ModernForum_EnsureSchema;
  2. EXEC dbo.ModernForum_ImportCategories;
  3. EXEC dbo.ModernForum_ImportThreads @BatchSize = 250, @MaxBatches = 1;
     Repeat step 3 until RowsImported = 0.
  4. EXEC dbo.ModernForum_ImportPosts @BatchSize = 250, @MaxBatches = 1;
     Repeat step 4 until RowsImported = 0.

Notes:
  - Keep @MaxBatches low on 5 DTU so each call commits quickly.
  - Lower @BatchSize to 50-100 if TOPIC_MESSAGE rows are causing timeouts.
  - Import all threads before posts. Replies are attached to imported parent
    threads and use the post checkpoint only after their thread exists.

Live run:
  Applied to queenzone-db on 2026-06-29 while the database was on the 5 DTU
  tier. Threads completed at 5,969 rows; posts completed at 68,208 rows.
  Final checkpoints for ModernForum.Threads and ModernForum.Posts both had
  LastRowsImported = 0.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_EnsureSchema
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF OBJECT_ID(N'dbo.ImportCheckpoint', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ImportCheckpoint
        (
            Name nvarchar(100) NOT NULL CONSTRAINT PK_ImportCheckpoint PRIMARY KEY,
            LastLegacyId int NOT NULL CONSTRAINT DF_ImportCheckpoint_LastLegacyId DEFAULT (0),
            LastRunStartedAt datetime2(0) NULL,
            LastRunCompletedAt datetime2(0) NULL,
            LastRowsImported int NOT NULL CONSTRAINT DF_ImportCheckpoint_LastRowsImported DEFAULT (0),
            UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_ImportCheckpoint_UpdatedAt DEFAULT (sysutcdatetime())
        );
    END;

    IF OBJECT_ID(N'dbo.ModernForumCategory', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ModernForumCategory
        (
            Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModernForumCategory PRIMARY KEY,
            LegacyForumId int NOT NULL,
            Name nvarchar(100) NOT NULL,
            Description nvarchar(400) NULL,
            SortOrder int NOT NULL,
            LegacyPostCount int NOT NULL,
            LastActivityAt datetime2(0) NULL,
            ImportedAt datetime2(0) NOT NULL CONSTRAINT DF_ModernForumCategory_ImportedAt DEFAULT (sysutcdatetime()),
            UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_ModernForumCategory_UpdatedAt DEFAULT (sysutcdatetime()),
            CONSTRAINT UQ_ModernForumCategory_LegacyForumId UNIQUE (LegacyForumId)
        );
    END;

    IF OBJECT_ID(N'dbo.ModernForumThread', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ModernForumThread
        (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModernForumThread PRIMARY KEY,
            LegacyTopicId int NOT NULL,
            LegacyForumId int NOT NULL,
            CategoryId int NOT NULL,
            Title nvarchar(200) NOT NULL,
            StartedByLegacyUserId int NULL,
            StartedByDisplayName nvarchar(100) NOT NULL,
            StartedAt datetime2(0) NULL,
            LastActivityAt datetime2(0) NULL,
            ReplyCount int NOT NULL,
            IsSticky bit NOT NULL,
            ImportedAt datetime2(0) NOT NULL CONSTRAINT DF_ModernForumThread_ImportedAt DEFAULT (sysutcdatetime()),
            UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_ModernForumThread_UpdatedAt DEFAULT (sysutcdatetime()),
            CONSTRAINT UQ_ModernForumThread_LegacyTopicId UNIQUE (LegacyTopicId),
            CONSTRAINT FK_ModernForumThread_Category FOREIGN KEY (CategoryId)
                REFERENCES dbo.ModernForumCategory (Id)
        );

        CREATE INDEX IX_ModernForumThread_Category_LastActivity
        ON dbo.ModernForumThread (CategoryId, IsSticky DESC, LastActivityAt DESC, LegacyTopicId ASC)
        INCLUDE (Title, StartedByDisplayName, ReplyCount);
    END;

    IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ModernForumPost
        (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModernForumPost PRIMARY KEY,
            LegacyPostId int NOT NULL,
            LegacyThreadTopicId int NOT NULL,
            ThreadId bigint NOT NULL,
            LegacyForumId int NOT NULL,
            AuthorLegacyUserId int NULL,
            AuthorDisplayName nvarchar(100) NOT NULL,
            AuthorPostCount int NULL,
            AuthorJoinedAt datetime2(0) NULL,
            BodyHtml varchar(8000) NOT NULL,
            SignatureHtml varchar(8000) NULL,
            PostedAt datetime2(0) NULL,
            ImportedAt datetime2(0) NOT NULL CONSTRAINT DF_ModernForumPost_ImportedAt DEFAULT (sysutcdatetime()),
            UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_ModernForumPost_UpdatedAt DEFAULT (sysutcdatetime()),
            CONSTRAINT UQ_ModernForumPost_LegacyPostId UNIQUE (LegacyPostId),
            CONSTRAINT FK_ModernForumPost_Thread FOREIGN KEY (ThreadId)
                REFERENCES dbo.ModernForumThread (Id)
        );

        CREATE INDEX IX_ModernForumPost_Thread_Posted
        ON dbo.ModernForumPost (ThreadId, LegacyPostId ASC)
        INCLUDE (PostedAt, AuthorDisplayName);
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_ImportCategories
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    EXEC dbo.ModernForum_EnsureSchema;

    MERGE dbo.ModernForumCategory AS target
    USING
    (
        SELECT
            CAST(Q_FORUM_ID AS int) AS LegacyForumId,
            CONVERT(nvarchar(100), LTRIM(RTRIM(ISNULL(Q_FORUM_NAME, '')))) AS Name,
            NULLIF(CONVERT(nvarchar(400), LTRIM(RTRIM(ISNULL(Q_FORUM_DESCRIPTION, '')))), '') AS Description,
            ISNULL(CAST(FORUM_ORDER AS int), 0) AS SortOrder,
            ISNULL(Q_FORUM_POST_COUNT, 0) AS LegacyPostCount,
            CAST(Q_FORUM_LAST_POST AS datetime2(0)) AS LastActivityAt
        FROM dbo.Q_FORUM_T
        WHERE LTRIM(RTRIM(ISNULL(Q_FORUM_NAME, ''))) <> ''
    ) AS source
    ON target.LegacyForumId = source.LegacyForumId
    WHEN MATCHED THEN
        UPDATE SET
            Name = source.Name,
            Description = source.Description,
            SortOrder = source.SortOrder,
            LegacyPostCount = source.LegacyPostCount,
            LastActivityAt = source.LastActivityAt,
            UpdatedAt = sysutcdatetime()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (LegacyForumId, Name, Description, SortOrder, LegacyPostCount, LastActivityAt)
        VALUES (source.LegacyForumId, source.Name, source.Description, source.SortOrder, source.LegacyPostCount, source.LastActivityAt);
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_ImportThreads
    @BatchSize int = 250,
    @MaxBatches int = 1,
    @DelayMilliseconds int = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    EXEC dbo.ModernForum_EnsureSchema;

    SET @BatchSize = CASE WHEN @BatchSize BETWEEN 1 AND 5000 THEN @BatchSize ELSE 250 END;
    SET @MaxBatches = CASE WHEN @MaxBatches BETWEEN 1 AND 1000 THEN @MaxBatches ELSE 1 END;
    SET @DelayMilliseconds = CASE WHEN @DelayMilliseconds BETWEEN 0 AND 60000 THEN @DelayMilliseconds ELSE 0 END;

    DECLARE @checkpointName nvarchar(100) = N'ModernForum.Threads';
    DECLARE @batch int = 0;
    DECLARE @rows int = 1;
    DECLARE @totalRows int = 0;
    DECLARE @lastLegacyId int;
    DECLARE @startedAt datetime2(0);
    DECLARE @threadBatch TABLE
    (
        LegacyTopicId int NOT NULL PRIMARY KEY,
        LegacyForumId int NOT NULL,
        CategoryId int NOT NULL,
        Title nvarchar(200) NOT NULL,
        StartedByLegacyUserId int NULL,
        StartedByDisplayName nvarchar(100) NOT NULL,
        StartedAt datetime2(0) NULL,
        LastActivityAt datetime2(0) NULL,
        ReplyCount int NOT NULL,
        IsSticky bit NOT NULL
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportCheckpoint WHERE Name = @checkpointName)
    BEGIN
        INSERT dbo.ImportCheckpoint (Name) VALUES (@checkpointName);
    END;

    WHILE @batch < @MaxBatches AND @rows > 0
    BEGIN
        SELECT @lastLegacyId = LastLegacyId
        FROM dbo.ImportCheckpoint WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = @checkpointName;

        SET @startedAt = sysutcdatetime();
        DELETE FROM @threadBatch;

        INSERT @threadBatch
        (
            LegacyTopicId,
            LegacyForumId,
            CategoryId,
            Title,
            StartedByLegacyUserId,
            StartedByDisplayName,
            StartedAt,
            LastActivityAt,
            ReplyCount,
            IsSticky
        )
        SELECT TOP (@BatchSize)
            t.Q_FORUM_TOPIC_ID,
            CAST(t.Q_FORUM_ID AS int),
            c.Id,
            CONVERT(nvarchar(200), LTRIM(RTRIM(t.TOPIC_SUBJECT))),
            t.USER_ID,
            COALESCE(NULLIF(CONVERT(nvarchar(100), LTRIM(RTRIM(u.USERNAME))), ''), N'Unknown'),
            CAST(t.TOPIC_DATE AS datetime2(0)),
            CAST(t.TOPIC_LAST_POST AS datetime2(0)),
            ISNULL(CAST(t.TOPIC_REPLIES AS int), 0),
            CASE WHEN ISNULL(t.STICKY, 0) = 1 THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END
        FROM dbo.Q_FORUM_TOPIC_T t
        INNER JOIN dbo.ModernForumCategory c
            ON c.LegacyForumId = CAST(t.Q_FORUM_ID AS int)
        LEFT JOIN dbo.USERS_T u
            ON u.USER_ID = t.USER_ID
        WHERE t.Q_FORUM_TOPIC_ID > @lastLegacyId
          AND t.Q_FORUM_TOPIC_PARENT_ID = 0
          AND LTRIM(RTRIM(ISNULL(t.TOPIC_SUBJECT, ''))) <> ''
          AND ISNULL(t.DISCOGRAPHY, 0) <> 2
        ORDER BY t.Q_FORUM_TOPIC_ID ASC;

        BEGIN TRANSACTION;

        INSERT dbo.ModernForumThread
        (
            LegacyTopicId,
            LegacyForumId,
            CategoryId,
            Title,
            StartedByLegacyUserId,
            StartedByDisplayName,
            StartedAt,
            LastActivityAt,
            ReplyCount,
            IsSticky
        )
        SELECT
            b.LegacyTopicId,
            b.LegacyForumId,
            b.CategoryId,
            b.Title,
            b.StartedByLegacyUserId,
            b.StartedByDisplayName,
            b.StartedAt,
            b.LastActivityAt,
            b.ReplyCount,
            b.IsSticky
        FROM @threadBatch b
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.ModernForumThread existing
            WHERE existing.LegacyTopicId = b.LegacyTopicId
        );

        SET @rows = @@ROWCOUNT;

        UPDATE dbo.ImportCheckpoint
        SET
            LastLegacyId = ISNULL((SELECT MAX(LegacyTopicId) FROM @threadBatch), LastLegacyId),
            LastRunStartedAt = @startedAt,
            LastRunCompletedAt = sysutcdatetime(),
            LastRowsImported = @rows,
            UpdatedAt = sysutcdatetime()
        WHERE Name = @checkpointName;

        COMMIT TRANSACTION;

        SET @totalRows += @rows;
        SET @batch += 1;

        IF @rows > 0 AND @DelayMilliseconds > 0
        BEGIN
            DECLARE @delay varchar(12) = CONVERT(varchar(12), DATEADD(millisecond, @DelayMilliseconds, CONVERT(time(3), '00:00:00.000')), 114);
            WAITFOR DELAY @delay;
        END;
    END;

    SELECT @totalRows AS RowsImported, @batch AS BatchesRun;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ModernForum_ImportPosts
    @BatchSize int = 250,
    @MaxBatches int = 1,
    @DelayMilliseconds int = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    EXEC dbo.ModernForum_EnsureSchema;

    IF EXISTS (
        SELECT 1
        FROM dbo.ImportCheckpoint
        WHERE Name = N'ModernForum.Threads'
          AND LastRowsImported <> 0
    )
    BEGIN
        RAISERROR('ModernForum_ImportPosts requires ModernForum_ImportThreads to finish first. Repeat thread imports until RowsImported = 0, then run posts.', 16, 1);
        RETURN;
    END;

    SET @BatchSize = CASE WHEN @BatchSize BETWEEN 1 AND 5000 THEN @BatchSize ELSE 250 END;
    SET @MaxBatches = CASE WHEN @MaxBatches BETWEEN 1 AND 1000 THEN @MaxBatches ELSE 1 END;
    SET @DelayMilliseconds = CASE WHEN @DelayMilliseconds BETWEEN 0 AND 60000 THEN @DelayMilliseconds ELSE 0 END;

    DECLARE @checkpointName nvarchar(100) = N'ModernForum.Posts';
    DECLARE @batch int = 0;
    DECLARE @rows int = 1;
    DECLARE @totalRows int = 0;
    DECLARE @lastLegacyId int;
    DECLARE @startedAt datetime2(0);
    DECLARE @postBatch TABLE
    (
        LegacyPostId int NOT NULL PRIMARY KEY,
        LegacyThreadTopicId int NOT NULL,
        ThreadId bigint NOT NULL,
        LegacyForumId int NOT NULL,
        AuthorLegacyUserId int NULL,
        AuthorDisplayName nvarchar(100) NOT NULL,
        AuthorPostCount int NULL,
        AuthorJoinedAt datetime2(0) NULL,
        BodyHtml varchar(8000) NOT NULL,
        SignatureHtml varchar(8000) NULL,
        PostedAt datetime2(0) NULL
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportCheckpoint WHERE Name = @checkpointName)
    BEGIN
        INSERT dbo.ImportCheckpoint (Name) VALUES (@checkpointName);
    END;

    WHILE @batch < @MaxBatches AND @rows > 0
    BEGIN
        SELECT @lastLegacyId = LastLegacyId
        FROM dbo.ImportCheckpoint WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = @checkpointName;

        SET @startedAt = sysutcdatetime();
        DELETE FROM @postBatch;

        INSERT @postBatch
        (
            LegacyPostId,
            LegacyThreadTopicId,
            ThreadId,
            LegacyForumId,
            AuthorLegacyUserId,
            AuthorDisplayName,
            AuthorPostCount,
            AuthorJoinedAt,
            BodyHtml,
            SignatureHtml,
            PostedAt
        )
        SELECT TOP (@BatchSize)
            t.Q_FORUM_TOPIC_ID,
            mt.LegacyTopicId,
            mt.Id,
            CAST(t.Q_FORUM_ID AS int),
            t.USER_ID,
            COALESCE(NULLIF(CONVERT(nvarchar(100), LTRIM(RTRIM(u.USERNAME))), ''), N'Unknown'),
            CAST(u.NUMBER_OF_POSTS AS int),
            CAST(u.DATE_CREATED AS datetime2(0)),
            ISNULL(t.TOPIC_MESSAGE, ''),
            NULLIF(u.SIGNATURE, ''),
            CAST(t.TOPIC_DATE AS datetime2(0))
        FROM dbo.Q_FORUM_TOPIC_T t
        INNER JOIN dbo.ModernForumThread mt
            ON mt.LegacyTopicId = CASE
                WHEN t.Q_FORUM_TOPIC_PARENT_ID = 0 THEN t.Q_FORUM_TOPIC_ID
                ELSE t.Q_FORUM_TOPIC_PARENT_ID
            END
        LEFT JOIN dbo.USERS_T u
            ON u.USER_ID = t.USER_ID
        WHERE t.Q_FORUM_TOPIC_ID > @lastLegacyId
          AND ISNULL(t.DISCOGRAPHY, 0) <> 2
          AND ISNULL(t.TOPIC_MESSAGE, '') <> ''
        ORDER BY t.Q_FORUM_TOPIC_ID ASC;

        BEGIN TRANSACTION;

        INSERT dbo.ModernForumPost
        (
            LegacyPostId,
            LegacyThreadTopicId,
            ThreadId,
            LegacyForumId,
            AuthorLegacyUserId,
            AuthorDisplayName,
            AuthorPostCount,
            AuthorJoinedAt,
            BodyHtml,
            SignatureHtml,
            PostedAt
        )
        SELECT
            b.LegacyPostId,
            b.LegacyThreadTopicId,
            b.ThreadId,
            b.LegacyForumId,
            b.AuthorLegacyUserId,
            b.AuthorDisplayName,
            b.AuthorPostCount,
            b.AuthorJoinedAt,
            b.BodyHtml,
            b.SignatureHtml,
            b.PostedAt
        FROM @postBatch b
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.ModernForumPost existing
            WHERE existing.LegacyPostId = b.LegacyPostId
        );

        SET @rows = @@ROWCOUNT;

        UPDATE dbo.ImportCheckpoint
        SET
            LastLegacyId = ISNULL((SELECT MAX(LegacyPostId) FROM @postBatch), LastLegacyId),
            LastRunStartedAt = @startedAt,
            LastRunCompletedAt = sysutcdatetime(),
            LastRowsImported = @rows,
            UpdatedAt = sysutcdatetime()
        WHERE Name = @checkpointName;

        COMMIT TRANSACTION;

        SET @totalRows += @rows;
        SET @batch += 1;

        IF @rows > 0 AND @DelayMilliseconds > 0
        BEGIN
            DECLARE @delay varchar(12) = CONVERT(varchar(12), DATEADD(millisecond, @DelayMilliseconds, CONVERT(time(3), '00:00:00.000')), 114);
            WAITFOR DELAY @delay;
        END;
    END;

    SELECT @totalRows AS RowsImported, @batch AS BatchesRun;
END;
GO

/*
Progress checks:

SELECT * FROM dbo.ImportCheckpoint WHERE Name LIKE N'ModernForum.%';

SELECT COUNT(*) AS Categories FROM dbo.ModernForumCategory;
SELECT COUNT(*) AS Threads FROM dbo.ModernForumThread;
SELECT COUNT(*) AS Posts FROM dbo.ModernForumPost;

Rollback for proof-of-concept environments:

DROP PROCEDURE IF EXISTS dbo.ModernForum_ImportPosts;
DROP PROCEDURE IF EXISTS dbo.ModernForum_ImportThreads;
DROP PROCEDURE IF EXISTS dbo.ModernForum_ImportCategories;
DROP PROCEDURE IF EXISTS dbo.ModernForum_EnsureSchema;
DROP TABLE IF EXISTS dbo.ModernForumPost;
DROP TABLE IF EXISTS dbo.ModernForumThread;
DROP TABLE IF EXISTS dbo.ModernForumCategory;
DELETE FROM dbo.ImportCheckpoint WHERE Name IN (N'ModernForum.Threads', N'ModernForum.Posts');
*/
