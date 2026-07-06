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
                    { 1, "Freddie Mercury born", "Freddie Mercury, Queen's lead vocalist and pianist, was born in Stone Town, Zanzibar.", new DateTime(1946, 9, 5, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 95, "Wikipedia", "freddie-mercury-born-1946-09-05", "https://en.wikipedia.org/wiki/Freddie_Mercury", createdAt, true, createdAt, createdAt },
                    { 2, "Brian May born", "Brian May, Queen's lead guitarist, was born in Hampton, Middlesex, England.", new DateTime(1947, 7, 19, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 85, "Wikipedia", "brian-may-born-1947-07-19", "https://en.wikipedia.org/wiki/Brian_May", createdAt, true, createdAt, createdAt },
                    { 3, "Roger Taylor born", "Roger Taylor, Queen's drummer, was born in King's Lynn, Norfolk, England.", new DateTime(1949, 7, 26, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 85, "Wikipedia", "roger-taylor-born-1949-07-26", "https://en.wikipedia.org/wiki/Roger_Taylor_(musician)", createdAt, true, createdAt, createdAt },
                    { 4, "John Deacon born", "John Deacon, Queen's bassist, was born in Leicester, England.", new DateTime(1951, 8, 19, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Birthday", 85, "Wikipedia", "john-deacon-born-1951-08-19", "https://en.wikipedia.org/wiki/John_Deacon", createdAt, true, createdAt, createdAt },
                    { 5, "Queen's Live Aid performance", "Queen perform a 21-minute set at Live Aid at Wembley Stadium, later widely acclaimed as one of rock's greatest live performances.", new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Concert", 100, "Wikipedia", "queen-s-live-aid-performance-1985-07-13", "https://en.wikipedia.org/wiki/Live_Aid", createdAt, true, createdAt, createdAt },
                    { 6, "'Bohemian Rhapsody' released", "Queen release 'Bohemian Rhapsody' as a single in the UK.", new DateTime(1975, 10, 31, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Release", 100, "Wikipedia", "bohemian-rhapsody-released-1975-10-31", "https://en.wikipedia.org/wiki/Bohemian_Rhapsody", createdAt, true, createdAt, createdAt },
                    { 7, "The Freddie Mercury Tribute Concert", "The Freddie Mercury Tribute Concert for AIDS Awareness is held at Wembley Stadium.", new DateTime(1992, 4, 20, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Concert", 95, "Wikipedia", "the-freddie-mercury-tribute-concert-1992-04-20", "https://en.wikipedia.org/wiki/The_Freddie_Mercury_Tribute_Concert", createdAt, true, createdAt, createdAt },
                    { 8, "Queen released", "Queen's self-titled debut album is released in the UK.", new DateTime(1973, 7, 13, 0, 0, 0, DateTimeKind.Utc), "ExactDate", "Release", 85, "Wikipedia", "queen-released-1973-07-13", "https://en.wikipedia.org/wiki/Queen_(Queen_album)", createdAt, true, createdAt, createdAt },
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
