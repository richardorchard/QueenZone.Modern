using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberAccountSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "MemberAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "MemberAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendedByAdminEmail",
                table: "MemberAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendedReason",
                table: "MemberAccounts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccounts_IsSuspended",
                table: "MemberAccounts",
                column: "IsSuspended");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberAccounts_IsSuspended",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "SuspendedByAdminEmail",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "SuspendedReason",
                table: "MemberAccounts");
        }
    }
}
