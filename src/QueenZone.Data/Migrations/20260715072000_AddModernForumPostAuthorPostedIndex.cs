using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds the index needed by forum write rate-limit checks on the imported post table.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260715072000_AddModernForumPostAuthorPostedIndex")]
public partial class AddModernForumPostAuthorPostedIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorDisplayName_PostedAt')
            BEGIN
                CREATE INDEX IX_ModernForumPost_AuthorDisplayName_PostedAt
                    ON dbo.ModernForumPost (AuthorDisplayName, PostedAt);
            END
            """, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorDisplayName_PostedAt')
            BEGIN
                DROP INDEX IX_ModernForumPost_AuthorDisplayName_PostedAt
                    ON dbo.ModernForumPost;
            END
            """, suppressTransaction: true);
    }
}
