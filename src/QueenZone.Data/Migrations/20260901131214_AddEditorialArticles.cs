using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEditorialArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EditorialArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegacyArticleId = table.Column<int>(type: "int", nullable: true),
                    SourceSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageBlobKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    LiveTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LiveSlug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LiveExcerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LiveBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LiveAuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LiveCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LiveTags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LiveSource = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LiveImageBlobKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LivePublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorialArticles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditorialArticles_LegacyArticleId",
                table: "EditorialArticles",
                column: "LegacyArticleId",
                unique: true,
                filter: "[LegacyArticleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EditorialArticles_LiveSlug",
                table: "EditorialArticles",
                column: "LiveSlug",
                unique: true,
                filter: "[LiveSlug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EditorialArticles_Slug",
                table: "EditorialArticles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EditorialArticles_SourceSubmissionId",
                table: "EditorialArticles",
                column: "SourceSubmissionId",
                unique: true,
                filter: "[SourceSubmissionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditorialArticles");
        }
    }
}
