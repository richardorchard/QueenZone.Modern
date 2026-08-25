using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateMessageReportReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "PrivateMessageReports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "PrivateMessageReports",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerEmail",
                table: "PrivateMessageReports",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrivateMessageReportAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateMessageReportAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateMessageReportAuditLog_PrivateMessageReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "PrivateMessageReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessageReportAuditLog_Report_OccurredAt",
                table: "PrivateMessageReportAuditLog",
                columns: new[] { "ReportId", "OccurredAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateMessageReportAuditLog");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "PrivateMessageReports");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "PrivateMessageReports");

            migrationBuilder.DropColumn(
                name: "ReviewerEmail",
                table: "PrivateMessageReports");
        }
    }
}
