using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateMessageSenderIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrivateMessages_SenderMemberId",
                table: "PrivateMessages");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_Sender_CreatedAt",
                table: "PrivateMessages",
                columns: new[] { "SenderMemberId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrivateMessages_Sender_CreatedAt",
                table: "PrivateMessages");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_SenderMemberId",
                table: "PrivateMessages",
                column: "SenderMemberId");
        }
    }
}
