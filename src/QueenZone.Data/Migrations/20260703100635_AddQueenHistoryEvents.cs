using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueenHistoryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueenHistoryEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatePrecision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueenHistoryEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueenHistoryEvents_Published_Date",
                table: "QueenHistoryEvents",
                columns: new[] { "IsPublished", "DatePrecision", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_QueenHistoryEvents_Source",
                table: "QueenHistoryEvents",
                columns: new[] { "SourceType", "SourceKey" },
                unique: true);

            var createdAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "QueenHistoryEvents",
                columns: new[]
                {
                    "Id",
                    "Title",
                    "Summary",
                    "EventDate",
                    "DatePrecision",
                    "Category",
                    "Importance",
                    "SourceType",
                    "SourceKey",
                    "SourceUrl",
                    "VerifiedAt",
                    "IsPublished",
                    "CreatedAt",
                    "UpdatedAt",
                },
                values: new object[,]
                {
                    { 1, "Freddie Mercury is born", "Farrokh Bulsara, later known as Freddie Mercury, is born in Zanzibar.", new DateTime(1946, 9, 5, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 95, "Curated", "curated:1", null, createdAt, true, createdAt, createdAt },
                    { 2, "Brian May is born", "Brian May is born in Hampton, Middlesex.", new DateTime(1947, 7, 19, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 90, "Curated", "curated:2", null, createdAt, true, createdAt, createdAt },
                    { 3, "Roger Taylor is born", "Roger Taylor is born in King's Lynn, Norfolk.", new DateTime(1949, 7, 26, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 90, "Curated", "curated:3", null, createdAt, true, createdAt, createdAt },
                    { 4, "John Deacon is born", "John Deacon is born in Leicester.", new DateTime(1951, 8, 19, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 90, "Curated", "curated:4", null, createdAt, true, createdAt, createdAt },
                    { 5, "Queen perform at Live Aid", "Queen play their celebrated Wembley Stadium set at Live Aid.", new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Concert", 100, "Curated", "curated:5", null, createdAt, true, createdAt, createdAt },
                    { 6, "Bohemian Rhapsody is released", "Bohemian Rhapsody is released as a single in the UK.", new DateTime(1975, 10, 31, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Release", 100, "Curated", "curated:6", null, createdAt, true, createdAt, createdAt },
                    { 7, "The Freddie Mercury Tribute Concert", "The Freddie Mercury Tribute Concert is held at Wembley Stadium.", new DateTime(1992, 4, 20, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Concert", 95, "Curated", "curated:7", null, createdAt, true, createdAt, createdAt },
                    { 8, "Queen release their debut album", "Queen release their self-titled debut album in the UK.", new DateTime(1973, 7, 13, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Release", 85, "Curated", "curated:8", null, createdAt, true, createdAt, createdAt },
                    { 9, "John Deacon joins Queen", "John Deacon joins Brian May, Freddie Mercury and Roger Taylor, completing Queen's classic line-up.", new DateTime(1971, 3, 1, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Other", 80, "Curated", "curated:9", null, createdAt, true, createdAt, createdAt },
                    { 10, "QueenZone modernisation milestone", "The modern QueenZone rebuild tracks archive-first development and public restoration work.", new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "SiteHistory", 60, "Curated", "curated:10", null, createdAt, true, createdAt, createdAt },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueenHistoryEvents");
        }
    }
}
