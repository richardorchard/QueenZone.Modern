using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFanPerformanceReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FanPerformanceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    ReporterMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PerformedBySnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FanPerformanceReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FanPerformanceReports_MemberAccounts_ReporterMemberId",
                        column: x => x.ReporterMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FanPerformanceReports_Status_CreatedAt",
                table: "FanPerformanceReports",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UX_FanPerformanceReports_Reporter_Stage_Open",
                table: "FanPerformanceReports",
                columns: new[] { "ReporterMemberId", "StageId" },
                unique: true,
                filter: "[Status] = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FanPerformanceReports");
        }
    }
}
