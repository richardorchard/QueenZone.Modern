using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchReindexLeasesAndRunRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchReindexLeases",
                columns: table => new
                {
                    LeaseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HolderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchReindexLeases", x => x.LeaseName);
                });

            migrationBuilder.CreateTable(
                name: "SearchReindexRunRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RunnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActiveKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchReindexRunRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchReindexRunRequests_Status_RequestedAtUtc",
                table: "SearchReindexRunRequests",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SearchReindexRunRequests_ActiveKey",
                table: "SearchReindexRunRequests",
                column: "ActiveKey",
                unique: true,
                filter: "[ActiveKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchReindexLeases");

            migrationBuilder.DropTable(
                name: "SearchReindexRunRequests");
        }
    }
}
