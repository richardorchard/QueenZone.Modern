using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFanPerformanceSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FanPerformanceSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmitterMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CoveredSong = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BlobPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RightsDeclaredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RightsDeclarationVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PromotedStageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FanPerformanceSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FanPerformanceSubmissions_MemberAccounts_SubmitterMemberId",
                        column: x => x.SubmitterMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FanPerformanceSubmissionAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FanPerformanceSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FanPerformanceSubmissionAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FanPerformanceSubmissionAuditLog_FanPerformanceSubmissions_FanPerformanceSubmissionId",
                        column: x => x.FanPerformanceSubmissionId,
                        principalTable: "FanPerformanceSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FanPerformanceSubmissionAuditLog_Submission_OccurredAt",
                table: "FanPerformanceSubmissionAuditLog",
                columns: new[] { "FanPerformanceSubmissionId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FanPerformanceSubmissions_Status_SubmittedAt",
                table: "FanPerformanceSubmissions",
                columns: new[] { "Status", "SubmittedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FanPerformanceSubmissions_Submitter_SubmittedAt",
                table: "FanPerformanceSubmissions",
                columns: new[] { "SubmitterMemberId", "SubmittedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FanPerformanceSubmissionAuditLog");

            migrationBuilder.DropTable(
                name: "FanPerformanceSubmissions");
        }
    }
}
