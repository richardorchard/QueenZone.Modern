using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueenLinkChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueenLinkChecks",
                columns: table => new
                {
                    QueenFeaturedSiteId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastCheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsConfirmedDead = table.Column<bool>(type: "bit", nullable: false),
                    ConsecutiveFailureCount = table.Column<int>(type: "int", nullable: false),
                    LastStatusCode = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueenLinkChecks", x => x.QueenFeaturedSiteId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueenLinkChecks_IsConfirmedDead",
                table: "QueenLinkChecks",
                column: "IsConfirmedDead");

            migrationBuilder.CreateIndex(
                name: "IX_QueenLinkChecks_LastCheckedAtUtc",
                table: "QueenLinkChecks",
                column: "LastCheckedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueenLinkChecks");
        }
    }
}
