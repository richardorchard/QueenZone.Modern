using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberMessageBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberMessageBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockerMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockedMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberMessageBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberMessageBlocks_MemberAccounts_BlockedMemberId",
                        column: x => x.BlockedMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberMessageBlocks_MemberAccounts_BlockerMemberId",
                        column: x => x.BlockerMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberMessageBlocks_Blocked",
                table: "MemberMessageBlocks",
                column: "BlockedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberMessageBlocks_Blocker_Blocked",
                table: "MemberMessageBlocks",
                columns: new[] { "BlockerMemberId", "BlockedMemberId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberMessageBlocks");
        }
    }
}
