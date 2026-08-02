using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateMessageSortKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Identity column must be added via T-SQL so existing rows (if any) receive values.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateMessages', N'SortKey') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateMessages]
                        ADD [SortKey] bigint IDENTITY(1, 1) NOT NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateConversationParticipants', N'LastReadSortKey') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversationParticipants]
                        ADD [LastReadSortKey] bigint NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PrivateMessages_Conversation_SortKey'
                      AND object_id = OBJECT_ID(N'dbo.PrivateMessages'))
                BEGIN
                    CREATE INDEX [IX_PrivateMessages_Conversation_SortKey]
                        ON [dbo].[PrivateMessages] ([ConversationId], [SortKey]);
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
                    WHERE name = N'IX_PrivateMessages_Conversation_SortKey'
                      AND object_id = OBJECT_ID(N'dbo.PrivateMessages'))
                BEGIN
                    DROP INDEX [IX_PrivateMessages_Conversation_SortKey] ON [dbo].[PrivateMessages];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateMessages', N'SortKey') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateMessages] DROP COLUMN [SortKey];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateConversationParticipants', N'LastReadSortKey') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversationParticipants] DROP COLUMN [LastReadSortKey];
                END
                """);
        }
    }
}
