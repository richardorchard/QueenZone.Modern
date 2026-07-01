using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForumFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_ForumCatalog')
                    CREATE FULLTEXT CATALOG FT_ForumCatalog;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumThread'))
                    CREATE FULLTEXT INDEX ON dbo.ModernForumThread (Title)
                        KEY INDEX PK_ModernForumThread
                        ON FT_ForumCatalog
                        WITH CHANGE_TRACKING AUTO;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost'))
                    CREATE FULLTEXT INDEX ON dbo.ModernForumPost (Body)
                        KEY INDEX PK_ModernForumPost
                        ON FT_ForumCatalog
                        WITH CHANGE_TRACKING AUTO;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.ModernForum_SearchThreads
                    @Query        NVARCHAR(500),
                    @Offset       INT,
                    @PageSize     INT,
                    @TotalRecords INT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;

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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.ModernForum_SearchThreads;");

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost'))
                    DROP FULLTEXT INDEX ON dbo.ModernForumPost;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumThread'))
                    DROP FULLTEXT INDEX ON dbo.ModernForumThread;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_ForumCatalog')
                    DROP FULLTEXT CATALOG FT_ForumCatalog;
                """);
        }
    }
}
