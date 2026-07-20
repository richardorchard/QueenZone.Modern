using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmitterMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UrlHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PromotedNewsId = table.Column<int>(type: "int", nullable: true),
                    DuplicateCandidateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsSuggestions_MemberAccounts_SubmitterMemberId",
                        column: x => x.SubmitterMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsSuggestions_NewsCandidates_DuplicateCandidateId",
                        column: x => x.DuplicateCandidateId,
                        principalTable: "NewsCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsSuggestions_DuplicateCandidateId",
                table: "NewsSuggestions",
                column: "DuplicateCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsSuggestions_Status_SubmittedAt",
                table: "NewsSuggestions",
                columns: new[] { "Status", "SubmittedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NewsSuggestions_Submitter_SubmittedAt",
                table: "NewsSuggestions",
                columns: new[] { "SubmitterMemberId", "SubmittedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NewsSuggestions_UrlHash_Active",
                table: "NewsSuggestions",
                column: "UrlHash",
                unique: true,
                filter: "[Status] IN ('Pending', 'UnderReview')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsSuggestions");
        }
    }
}
