using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsAgentRunLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsAgentRunLeases",
                columns: table => new
                {
                    LeaseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HolderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsAgentRunLeases", x => x.LeaseName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsAgentRunLeases");
        }
    }
}
