using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds a BodyHtmlLegacyRaw column to ModernForumPost so the one-time BBCode-to-HTML
/// backfill can preserve each post's original stored text before overwriting BodyHtml.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260802073000_AddForumPostBodyHtmlLegacyRaw")]
public partial class AddForumPostBodyHtmlLegacyRaw : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'BodyHtmlLegacyRaw') IS NULL
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ADD BodyHtmlLegacyRaw varchar(8000) NULL;
                END;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'BodyHtmlLegacyRaw') IS NOT NULL
                    ALTER TABLE dbo.ModernForumPost DROP COLUMN BodyHtmlLegacyRaw;
            END
            """);
    }
}
