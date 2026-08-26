using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsAgentGuidanceRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuidanceContentHash",
                table: "NewsAiRuns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuidanceRevisionId",
                table: "NewsAiRuns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuidanceRevisionNumber",
                table: "NewsAiRuns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NewsAgentGuidanceRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsAgentGuidanceRevisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_NewsAgentGuidanceRevisions_Type_Draft",
                table: "NewsAgentGuidanceRevisions",
                column: "Type",
                unique: true,
                filter: "[Status] = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "UX_NewsAgentGuidanceRevisions_Type_Published",
                table: "NewsAgentGuidanceRevisions",
                column: "Type",
                unique: true,
                filter: "[Status] = 'Published'");

            migrationBuilder.CreateIndex(
                name: "UX_NewsAgentGuidanceRevisions_Type_RevisionNumber",
                table: "NewsAgentGuidanceRevisions",
                columns: new[] { "Type", "RevisionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsAgentGuidanceRevisions");

            migrationBuilder.DropColumn(
                name: "GuidanceContentHash",
                table: "NewsAiRuns");

            migrationBuilder.DropColumn(
                name: "GuidanceRevisionId",
                table: "NewsAiRuns");

            migrationBuilder.DropColumn(
                name: "GuidanceRevisionNumber",
                table: "NewsAiRuns");
        }
    }
}
