using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(QueenZoneDbContext))]
    [Migration("20260802140000_AddNewsAgentUrlIngestionRequests")]
    public class AddNewsAgentUrlIngestionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "NewsAgentRunRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "ScheduledGathering");

            migrationBuilder.AddColumn<string>(
                name: "ArticleUrl",
                table: "NewsAgentRunRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GenerateDraft",
                table: "NewsAgentRunRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "NewsAgentRunRequests");

            migrationBuilder.DropColumn(
                name: "ArticleUrl",
                table: "NewsAgentRunRequests");

            migrationBuilder.DropColumn(
                name: "GenerateDraft",
                table: "NewsAgentRunRequests");
        }
    }
}
