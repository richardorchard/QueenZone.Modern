using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileAuthGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileAuthAuthorizationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MemberAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileAuthAuthorizationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MobileAuthAuthorizationCodes_MemberAccounts_MemberAccountId",
                        column: x => x.MemberAccountId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MobileAuthRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MemberAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileAuthRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MobileAuthRefreshTokens_MemberAccounts_MemberAccountId",
                        column: x => x.MemberAccountId,
                        principalTable: "MemberAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobileAuthAuthorizationCodes_CodeHash",
                table: "MobileAuthAuthorizationCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobileAuthAuthorizationCodes_MemberAccountId",
                table: "MobileAuthAuthorizationCodes",
                column: "MemberAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileAuthRefreshTokens_Member_Revoked",
                table: "MobileAuthRefreshTokens",
                columns: new[] { "MemberAccountId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MobileAuthRefreshTokens_TokenHash",
                table: "MobileAuthRefreshTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileAuthAuthorizationCodes");

            migrationBuilder.DropTable(
                name: "MobileAuthRefreshTokens");
        }
    }
}
