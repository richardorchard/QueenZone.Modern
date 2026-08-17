using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberFollowsAndMessagePrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "MessagePrivacy",
                table: "MemberAccounts",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "MemberFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowerMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowedMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberFollows_MemberAccounts_FollowedMemberId",
                        column: x => x.FollowedMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberFollows_MemberAccounts_FollowerMemberId",
                        column: x => x.FollowerMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberFollows_Followed",
                table: "MemberFollows",
                column: "FollowedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFollows_Follower_Followed",
                table: "MemberFollows",
                columns: new[] { "FollowerMemberId", "FollowedMemberId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberFollows");

            migrationBuilder.DropColumn(
                name: "MessagePrivacy",
                table: "MemberAccounts");
        }
    }
}
