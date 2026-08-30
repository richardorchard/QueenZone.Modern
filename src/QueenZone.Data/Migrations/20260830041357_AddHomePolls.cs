using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomePolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomePolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePolls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomePollOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PollId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePollOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomePollOptions_HomePolls_PollId",
                        column: x => x.PollId,
                        principalTable: "HomePolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomePollVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PollId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VotedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePollVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomePollVotes_HomePollOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "HomePollOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomePollVotes_HomePolls_PollId",
                        column: x => x.PollId,
                        principalTable: "HomePolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomePollOptions_PollId_DisplayOrder",
                table: "HomePollOptions",
                columns: new[] { "PollId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_HomePolls_IsCurrent",
                table: "HomePolls",
                column: "IsCurrent",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_HomePollVotes_OptionId",
                table: "HomePollVotes",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "UQ_HomePollVotes_Poll_Member",
                table: "HomePollVotes",
                columns: new[] { "PollId", "MemberAccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomePollVotes");

            migrationBuilder.DropTable(
                name: "HomePollOptions");

            migrationBuilder.DropTable(
                name: "HomePolls");
        }
    }
}
