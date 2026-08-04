using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Drops ModernForumPost.BodyHtmlLegacyRaw. It held each legacy post's original text as a
/// rollback/audit trail for the one-time BBCode-to-HTML backfill; a full database backup was
/// taken and the converted production data has been spot-checked, so the column is no longer
/// needed and is dropped to reclaim space on the resource-constrained production tier.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260804040000_DropForumPostBodyHtmlLegacyRaw")]
public partial class DropForumPostBodyHtmlLegacyRaw : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.ModernForumPost', N'BodyHtmlLegacyRaw') IS NOT NULL
                    ALTER TABLE dbo.ModernForumPost DROP COLUMN BodyHtmlLegacyRaw;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
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
}
