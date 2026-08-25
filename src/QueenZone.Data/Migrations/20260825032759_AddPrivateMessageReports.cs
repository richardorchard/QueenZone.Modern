using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateMessageReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivateMessageReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageBodySnapshot = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SenderDisplayNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MessageCreatedAtSnapshot = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MessageSortKeySnapshot = table.Column<long>(type: "bigint", nullable: false),
                    PrecedingContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateMessageReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateMessageReports_MemberAccounts_ReportedMemberId",
                        column: x => x.ReportedMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateMessageReports_MemberAccounts_ReporterMemberId",
                        column: x => x.ReporterMemberId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateMessageReports_PrivateConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "PrivateConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateMessageReports_PrivateMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "PrivateMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessageReports_Conversation",
                table: "PrivateMessageReports",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessageReports_MessageId",
                table: "PrivateMessageReports",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessageReports_ReportedMemberId",
                table: "PrivateMessageReports",
                column: "ReportedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessageReports_Reporter_Message",
                table: "PrivateMessageReports",
                columns: new[] { "ReporterMemberId", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessageReports_Status_CreatedAt",
                table: "PrivateMessageReports",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateMessageReports");
        }
    }
}
