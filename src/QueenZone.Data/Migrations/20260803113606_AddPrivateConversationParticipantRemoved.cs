using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateConversationParticipantRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateConversationParticipants', N'IsRemoved') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversationParticipants]
                        ADD [IsRemoved] bit NOT NULL
                            CONSTRAINT [DF_PrivateConversationParticipants_IsRemoved] DEFAULT (0);
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PrivateConversationParticipants_Member_Removed'
                      AND object_id = OBJECT_ID(N'dbo.PrivateConversationParticipants'))
                BEGIN
                    CREATE INDEX [IX_PrivateConversationParticipants_Member_Removed]
                        ON [dbo].[PrivateConversationParticipants] ([MemberId], [IsRemoved]);
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'dbo.DF_PrivateConversationParticipants_IsRemoved', N'D') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversationParticipants]
                        DROP CONSTRAINT [DF_PrivateConversationParticipants_IsRemoved];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PrivateConversationParticipants_Member_Removed'
                      AND object_id = OBJECT_ID(N'dbo.PrivateConversationParticipants'))
                BEGIN
                    DROP INDEX [IX_PrivateConversationParticipants_Member_Removed]
                        ON [dbo].[PrivateConversationParticipants];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateConversationParticipants', N'IsRemoved') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversationParticipants] DROP COLUMN [IsRemoved];
                END
                """);
        }
    }
}
