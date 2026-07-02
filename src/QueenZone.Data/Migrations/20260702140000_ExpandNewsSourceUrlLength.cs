using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

public partial class ExpandNewsSourceUrlLength : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('NEWS_T', 'SOURCE_URL') IS NOT NULL
            BEGIN
                ALTER TABLE NEWS_T ALTER COLUMN SOURCE_URL VARCHAR(2000) NULL;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('NEWS_T', 'SOURCE_URL') IS NOT NULL
            BEGIN
                UPDATE NEWS_T
                SET SOURCE_URL = LEFT(SOURCE_URL, 75)
                WHERE SOURCE_URL IS NOT NULL AND LEN(SOURCE_URL) > 75;

                ALTER TABLE NEWS_T ALTER COLUMN SOURCE_URL VARCHAR(75) NULL;
            END;
            """);
    }
}
