using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260706113000_EnsureNewsSourceUrlLength")]
public partial class EnsureNewsSourceUrlLength : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'SOURCE_URL') IS NOT NULL
                AND COL_LENGTH('dbo.NEWS_T', 'SOURCE_URL') < 500
            BEGIN
                ALTER TABLE dbo.NEWS_T ALTER COLUMN SOURCE_URL VARCHAR(500) NULL;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.NEWS_T', 'SOURCE_URL') IS NOT NULL
                AND COL_LENGTH('dbo.NEWS_T', 'SOURCE_URL') > 75
            BEGIN
                UPDATE dbo.NEWS_T
                SET SOURCE_URL = LEFT(SOURCE_URL, 75)
                WHERE SOURCE_URL IS NOT NULL AND LEN(SOURCE_URL) > 75;

                ALTER TABLE dbo.NEWS_T ALTER COLUMN SOURCE_URL VARCHAR(75) NULL;
            END;
            """);
    }
}
