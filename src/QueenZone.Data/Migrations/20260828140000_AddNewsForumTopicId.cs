using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds nullable <c>FORUM_TOPIC_ID</c> on legacy <c>NEWS_T</c> with a unique filtered
/// index when set. The table is <c>ExcludeFromMigrations()</c>, so this is hand-written SQL.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260828140000_AddNewsForumTopicId")]
public partial class AddNewsForumTopicId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'FORUM_TOPIC_ID') IS NULL
            BEGIN
                ALTER TABLE dbo.NEWS_T ADD FORUM_TOPIC_ID INT NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_NEWS_T_ForumTopicId'
                  AND object_id = OBJECT_ID(N'dbo.NEWS_T'))
            BEGIN
                CREATE UNIQUE NONCLUSTERED INDEX IX_NEWS_T_ForumTopicId
                    ON dbo.NEWS_T (FORUM_TOPIC_ID)
                    WHERE FORUM_TOPIC_ID IS NOT NULL;
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_NEWS_T_ForumTopicId'
                  AND object_id = OBJECT_ID(N'dbo.NEWS_T'))
            BEGIN
                DROP INDEX IX_NEWS_T_ForumTopicId ON dbo.NEWS_T;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'FORUM_TOPIC_ID') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.NEWS_T DROP COLUMN FORUM_TOPIC_ID;
            END;
            """);
    }
}
