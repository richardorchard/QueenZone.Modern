using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260720090000_AddMemberLastLoginAt")]
public partial class AddMemberLastLoginAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.MemberAccounts', 'LastLoginAt') IS NULL
            BEGIN
                ALTER TABLE dbo.MemberAccounts ADD LastLoginAt datetime2 NULL;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.MemberAccounts', 'LastLoginAt') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.MemberAccounts DROP COLUMN LastLoginAt;
            END
            """);
    }
}
