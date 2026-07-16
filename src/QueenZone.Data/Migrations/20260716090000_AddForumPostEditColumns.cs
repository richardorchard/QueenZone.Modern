using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds author ownership and edit metadata columns for member post editing,
/// and updates ModernForum_GetTopicPostsPage to return them.
/// </summary>
/// <remarks>
/// SQL Server binds an entire batch before execution. ALTER TABLE ... ADD and a later
/// CREATE INDEX that references the new column must not share one Sql() batch, or you get
/// error 207 Invalid column name. Keep each dependent step in its own migrationBuilder.Sql call.
/// </remarks>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260716090000_AddForumPostEditColumns")]
public partial class AddForumPostEditColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'AuthorMemberId') IS NULL
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ADD AuthorMemberId uniqueidentifier NULL;
                END;

                IF COL_LENGTH(N'dbo.ModernForumPost', N'EditedAt') IS NULL
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ADD EditedAt datetime2 NULL;
                END;

                IF COL_LENGTH(N'dbo.ModernForumPost', N'EditCount') IS NULL
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ADD EditCount int NOT NULL
                            CONSTRAINT DF_ModernForumPost_EditCount DEFAULT (0);
                END;
            END
            """);

        // Separate batch: CREATE INDEX must see AuthorMemberId after ALTER has committed.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.ModernForumPost', N'AuthorMemberId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId')
            BEGIN
                CREATE INDEX IX_ModernForumPost_AuthorMemberId
                    ON dbo.ModernForumPost (AuthorMemberId)
                    WHERE AuthorMemberId IS NOT NULL;
            END
            """, suppressTransaction: true);

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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId')
                BEGIN
                    DROP INDEX IX_ModernForumPost_AuthorMemberId
                        ON dbo.ModernForumPost;
                END;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'EditCount') IS NOT NULL
                BEGIN
                    DECLARE @df sysname =
                    (
                        SELECT dc.name
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.default_object_id = dc.object_id
                           AND c.object_id = dc.parent_object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ModernForumPost')
                          AND c.name = N'EditCount'
                    );
                    IF @df IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.ModernForumPost DROP CONSTRAINT [' + @df + N']');

                    ALTER TABLE dbo.ModernForumPost DROP COLUMN EditCount;
                END;

                IF COL_LENGTH(N'dbo.ModernForumPost', N'EditedAt') IS NOT NULL
                    ALTER TABLE dbo.ModernForumPost DROP COLUMN EditedAt;

                IF COL_LENGTH(N'dbo.ModernForumPost', N'AuthorMemberId') IS NOT NULL
                    ALTER TABLE dbo.ModernForumPost DROP COLUMN AuthorMemberId;
            END
            """);
    }
}
