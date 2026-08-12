using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberAccountDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionRequestedAt",
                table: "MemberAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonalDataPurgedAt",
                table: "MemberAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberAccountDeletionAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAccountDeletionAuditLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccounts_DeletionRequestedAt_PersonalDataPurgedAt",
                table: "MemberAccounts",
                columns: new[] { "DeletionRequestedAt", "PersonalDataPurgedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccountDeletionAuditLog_MemberAccountId_OccurredAt",
                table: "MemberAccountDeletionAuditLog",
                columns: new[] { "MemberAccountId", "OccurredAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberAccountDeletionAuditLog");

            migrationBuilder.DropIndex(
                name: "IX_MemberAccounts_DeletionRequestedAt_PersonalDataPurgedAt",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedAt",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "PersonalDataPurgedAt",
                table: "MemberAccounts");
        }
    }
}
