using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Speeds forum creates: SQL sequences for legacy topic/post ids (replaces MAX+1 under
/// Serializable), and returns HasPoll from ModernForum_GetTopicPostsPage so topic pages
/// can skip a poll round-trip when none exists.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260716120000_ForumCreatePerfSequencesAndHasPoll")]
public partial class ForumCreatePerfSequencesAndHasPoll : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumThread', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'ForumLegacyTopicIdSeq' AND schema_id = SCHEMA_ID(N'dbo'))
            BEGIN
                DECLARE @topicStart int =
                    ISNULL((SELECT MAX(LegacyTopicId) FROM dbo.ModernForumThread), 0) + 1;
                DECLARE @topicSql nvarchar(200) =
                    N'CREATE SEQUENCE dbo.ForumLegacyTopicIdSeq AS int START WITH '
                    + CONVERT(nvarchar(11), @topicStart)
                    + N' INCREMENT BY 1 NO CACHE';
                EXEC sys.sp_executesql @topicSql;
            END;

            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'ForumLegacyPostIdSeq' AND schema_id = SCHEMA_ID(N'dbo'))
            BEGIN
                DECLARE @postStart int =
                    ISNULL((SELECT MAX(LegacyPostId) FROM dbo.ModernForumPost), 0) + 1;
                DECLARE @postSql nvarchar(200) =
                    N'CREATE SEQUENCE dbo.ForumLegacyPostIdSeq AS int START WITH '
                    + CONVERT(nvarchar(11), @postStart)
                    + N' INCREMENT BY 1 NO CACHE';
                EXEC sys.sp_executesql @postSql;
            END;
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicPostsPage
                @CurrentPage int,
                @PageSize int,
                @Q_FORUM_TOPIC_ID int,
                @TotalRecords int OUTPUT,
                @forum_name nvarchar(100) OUTPUT,
                @SUBJECT nvarchar(200) OUTPUT,
                @Q_FORUM_ID int OUTPUT,
                @DISCO tinyint OUTPUT,
                @HasPoll bit OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @Offset int = (CASE WHEN @CurrentPage > 1 THEN @CurrentPage - 1 ELSE 0 END) * @PageSize;
                DECLARE @ThreadId bigint;

                SET @HasPoll = 0;

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

                IF OBJECT_ID(N'dbo.ForumPolls', N'U') IS NOT NULL
                   AND EXISTS (SELECT 1 FROM dbo.ForumPolls WHERE LegacyTopicId = @Q_FORUM_TOPIC_ID)
                BEGIN
                    SET @HasPoll = 1;
                END;

                SELECT @TotalRecords = PostCount
                FROM dbo.ModernForumThreadReadStats
                WHERE ThreadId = @ThreadId;

                IF @TotalRecords IS NULL
                BEGIN
                    SELECT @TotalRecords = COUNT_BIG(*)
                    FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
                    WHERE p.ThreadId = @ThreadId;
                END;

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
                    p.LegacyDiscography AS DISCO,
                    p.AuthorMemberId,
                    p.EditedAt,
                    p.EditCount
                FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
                WHERE p.ThreadId = @ThreadId
                ORDER BY p.LegacyPostId ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            END;
            """, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'ForumLegacyTopicIdSeq' AND schema_id = SCHEMA_ID(N'dbo'))
                DROP SEQUENCE dbo.ForumLegacyTopicIdSeq;

            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'ForumLegacyPostIdSeq' AND schema_id = SCHEMA_ID(N'dbo'))
                DROP SEQUENCE dbo.ForumLegacyPostIdSeq;
            """);

        migrationBuilder.Sql("""
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

                SELECT @TotalRecords = PostCount
                FROM dbo.ModernForumThreadReadStats
                WHERE ThreadId = @ThreadId;

                IF @TotalRecords IS NULL
                BEGIN
                    SELECT @TotalRecords = COUNT_BIG(*)
                    FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
                    WHERE p.ThreadId = @ThreadId;
                END;

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
                    p.LegacyDiscography AS DISCO,
                    p.AuthorMemberId,
                    p.EditedAt,
                    p.EditCount
                FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
                WHERE p.ThreadId = @ThreadId
                ORDER BY p.LegacyPostId ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            END;
            """, suppressTransaction: true);
    }
}
