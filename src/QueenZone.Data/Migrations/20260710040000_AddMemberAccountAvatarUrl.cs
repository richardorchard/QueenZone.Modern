using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds MemberAccounts.AvatarUrl. Requires the [Migration] attribute so EF discovers it
/// (hand-written migrations without Designer still need the attribute — see EnsureNewsSourceUrlLength).
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260710040000_AddMemberAccountAvatarUrl")]
public partial class AddMemberAccountAvatarUrl : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent: safe if a previous deploy partially applied schema outside EF history.
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.MemberAccounts', 'AvatarUrl') IS NULL
            BEGIN
                ALTER TABLE dbo.MemberAccounts ADD AvatarUrl nvarchar(512) NULL;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.MemberAccounts', 'AvatarUrl') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.MemberAccounts DROP COLUMN AvatarUrl;
            END
            """);
    }
}
