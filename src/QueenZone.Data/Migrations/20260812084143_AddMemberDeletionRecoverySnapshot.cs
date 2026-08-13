using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberDeletionRecoverySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletionRecoveryAvatarUrl",
                table: "MemberAccounts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionRecoveryDisplayName",
                table: "MemberAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletionRecoveryAvatarUrl",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "DeletionRecoveryDisplayName",
                table: "MemberAccounts");
        }
    }
}
