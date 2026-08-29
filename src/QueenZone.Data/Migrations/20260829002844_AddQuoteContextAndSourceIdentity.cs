using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteContextAndSourceIdentity : Migration
    {
        // QUEEN_QUOTE_T is a legacy table (ToTable(..., ExcludeFromMigrations())), so
        // `dotnet ef migrations add` cannot diff it automatically -- these operations
        // are hand-written to match the table's existing varchar convention.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "QUEEN_QUOTE",
                table: "QUEEN_QUOTE_T",
                type: "varchar(1000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(455)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CONTEXT",
                table: "QUEEN_QUOTE_T",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SOURCE_TYPE",
                table: "QUEEN_QUOTE_T",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SOURCE_KEY",
                table: "QUEEN_QUOTE_T",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QUEEN_QUOTE_T_Source",
                table: "QUEEN_QUOTE_T",
                columns: new[] { "SOURCE_TYPE", "SOURCE_KEY" },
                unique: true,
                filter: "[SOURCE_TYPE] IS NOT NULL AND [SOURCE_KEY] IS NOT NULL");
        }

        /// <inheritdoc />
        // Note: shrinking QUEEN_QUOTE back to varchar(455) will fail with a truncation
        // error if any row's text is 456-1000 characters (that's the situation this
        // migration exists to allow). Check for and resolve such rows before rolling back.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QUEEN_QUOTE_T_Source",
                table: "QUEEN_QUOTE_T");

            migrationBuilder.DropColumn(
                name: "SOURCE_KEY",
                table: "QUEEN_QUOTE_T");

            migrationBuilder.DropColumn(
                name: "SOURCE_TYPE",
                table: "QUEEN_QUOTE_T");

            migrationBuilder.DropColumn(
                name: "CONTEXT",
                table: "QUEEN_QUOTE_T");

            migrationBuilder.AlterColumn<string>(
                name: "QUEEN_QUOTE",
                table: "QUEEN_QUOTE_T",
                type: "varchar(455)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldNullable: true);
        }
    }
}
