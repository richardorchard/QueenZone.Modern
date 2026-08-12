using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds a soft-hide flag to ModernForumPost so admin member suspension can hide a spamming
/// member's posts without deleting them, and updates ModernForum_GetTopicPostsPage to exclude
/// hidden posts from the public topic view.
/// </summary>
/// <remarks>
/// SQL Server binds an entire batch before execution. ALTER TABLE ... ADD must not share a
/// batch with statements referencing the new column, so the column add and the stored
/// procedure update are separate migrationBuilder.Sql calls (see AddForumPostEditColumns for
/// the same constraint). Attributes for this migration live in the generated Designer.cs
/// partial (needed here, unlike AddForumPostEditColumns, because this migration also adds a
/// new EF-tracked property and must keep the model snapshot in sync).
/// </remarks>
public partial class AddModernForumPostIsHidden : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'IsHidden') IS NULL
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ADD IsHidden bit NOT NULL
                            CONSTRAINT DF_ModernForumPost_IsHidden DEFAULT (0);
                END;
            END
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

                -- Cached PostCount includes hidden posts (refreshed by a periodic full sweep, not
                -- incrementally on hide/unhide); fall back to an exact filtered count when absent.
                SELECT @TotalRecords = PostCount
                FROM dbo.ModernForumThreadReadStats
                WHERE ThreadId = @ThreadId;

                IF @TotalRecords IS NULL
                BEGIN
                    SELECT @TotalRecords = COUNT_BIG(*)
                    FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
                    WHERE p.ThreadId = @ThreadId
                      AND p.IsHidden = 0;
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
                  AND p.IsHidden = 0
                ORDER BY p.LegacyPostId ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            END;
            """, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'IsHidden') IS NOT NULL
                BEGIN
                    DECLARE @df sysname =
                    (
                        SELECT dc.name
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.default_object_id = dc.object_id
                           AND c.object_id = dc.parent_object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ModernForumPost')
                          AND c.name = N'IsHidden'
                    );
                    IF @df IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.ModernForumPost DROP CONSTRAINT [' + @df + N']');

                    ALTER TABLE dbo.ModernForumPost DROP COLUMN IsHidden;
                END;
            END
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
}
