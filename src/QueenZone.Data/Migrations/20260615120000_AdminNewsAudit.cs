using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

public partial class AdminNewsAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('NEWS_T', 'SLUG') IS NULL
            BEGIN
                ALTER TABLE NEWS_T ADD SLUG NVARCHAR(200) NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('NEWS_T', 'CREATED_AT') IS NULL
            BEGIN
                ALTER TABLE NEWS_T ADD CREATED_AT DATETIME2(0) NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('NEWS_T', 'UPDATED_AT') IS NULL
            BEGIN
                ALTER TABLE NEWS_T ADD UPDATED_AT DATETIME2(0) NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('NEWS_T', 'EDITOR_EMAIL') IS NULL
            BEGIN
                ALTER TABLE NEWS_T ADD EDITOR_EMAIL NVARCHAR(256) NULL;
            END;
            """);

        migrationBuilder.CreateTable(
            name: "NewsAuditLog",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                NewsId = table.Column<int>(type: "int", nullable: false),
                Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                OccurredAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NewsAuditLog", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NewsAuditLog_NewsId_OccurredAt",
            table: "NewsAuditLog",
            columns: new[] { "NewsId", "OccurredAt" },
            descending: new[] { false, true });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NewsAuditLog");
    }
}