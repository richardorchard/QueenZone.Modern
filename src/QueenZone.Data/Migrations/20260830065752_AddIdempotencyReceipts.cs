using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationKind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ResponseBodyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyReceipts_ExpiresAt",
                table: "IdempotencyReceipts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyReceipts_Member_Kind_Operation",
                table: "IdempotencyReceipts",
                columns: new[] { "MemberId", "OperationKind", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyReceipts");
        }
    }
}
