using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds the index needed to list a legacy (unlinked) forum author's posts in chronological
/// order without scanning the whole imported post table.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260906090000_AddModernForumPostAuthorLegacyUserIndex")]
public partial class AddModernForumPostAuthorLegacyUserIndex : Migration
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
                      AND name = N'IX_ModernForumPost_AuthorLegacyUserId_PostedAt')
            BEGIN
                CREATE INDEX IX_ModernForumPost_AuthorLegacyUserId_PostedAt
                    ON dbo.ModernForumPost (AuthorLegacyUserId, PostedAt DESC)
                    INCLUDE (ThreadId, AuthorDisplayName, IsHidden)
                    WHERE AuthorLegacyUserId IS NOT NULL;
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
                      AND name = N'IX_ModernForumPost_AuthorLegacyUserId_PostedAt')
            BEGIN
                DROP INDEX IX_ModernForumPost_AuthorLegacyUserId_PostedAt
                    ON dbo.ModernForumPost;
            END
            """, suppressTransaction: true);
    }
}
