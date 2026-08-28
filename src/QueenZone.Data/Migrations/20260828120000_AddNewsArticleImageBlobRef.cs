using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds nullable image reference columns on legacy <c>NEWS_T</c>. The table is
/// <c>ExcludeFromMigrations()</c>, so a model-only migration would not change it.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260828120000_AddNewsArticleImageBlobRef")]
public partial class AddNewsArticleImageBlobRef : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'IMAGE_BLOB_KEY') IS NULL
            BEGIN
                ALTER TABLE dbo.NEWS_T ADD IMAGE_BLOB_KEY NVARCHAR(512) NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'IMAGE_GALLERY_PIC_ID') IS NULL
            BEGIN
                ALTER TABLE dbo.NEWS_T ADD IMAGE_GALLERY_PIC_ID INT NULL;
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'IMAGE_GALLERY_PIC_ID') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.NEWS_T DROP COLUMN IMAGE_GALLERY_PIC_ID;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'IMAGE_BLOB_KEY') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.NEWS_T DROP COLUMN IMAGE_BLOB_KEY;
            END;
            """);
    }
}
