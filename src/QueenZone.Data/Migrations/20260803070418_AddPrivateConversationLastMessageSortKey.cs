using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateConversationLastMessageSortKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateConversations', N'LastMessageSortKey') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversations]
                        ADD [LastMessageSortKey] bigint NOT NULL
                            CONSTRAINT [DF_PrivateConversations_LastMessageSortKey] DEFAULT (0);
                END
                """);

            migrationBuilder.Sql(
                """
                UPDATE c
                SET c.[LastMessageSortKey] = tip.[MaxSortKey]
                FROM [dbo].[PrivateConversations] AS c
                INNER JOIN (
                    SELECT [ConversationId], MAX([SortKey]) AS [MaxSortKey]
                    FROM [dbo].[PrivateMessages]
                    GROUP BY [ConversationId]
                ) AS tip ON tip.[ConversationId] = c.[Id]
                WHERE c.[LastMessageSortKey] <> tip.[MaxSortKey];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PrivateConversations_LastMessageAt'
                      AND object_id = OBJECT_ID(N'dbo.PrivateConversations'))
                BEGIN
                    DROP INDEX [IX_PrivateConversations_LastMessageAt]
                        ON [dbo].[PrivateConversations];
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PrivateConversations_LastMessageSortKey'
                      AND object_id = OBJECT_ID(N'dbo.PrivateConversations'))
                BEGIN
                    CREATE INDEX [IX_PrivateConversations_LastMessageSortKey]
                        ON [dbo].[PrivateConversations] ([LastMessageSortKey] DESC);
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'dbo.DF_PrivateConversations_LastMessageSortKey', N'D') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversations]
                        DROP CONSTRAINT [DF_PrivateConversations_LastMessageSortKey];
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
                    WHERE name = N'IX_PrivateConversations_LastMessageSortKey'
                      AND object_id = OBJECT_ID(N'dbo.PrivateConversations'))
                BEGIN
                    DROP INDEX [IX_PrivateConversations_LastMessageSortKey]
                        ON [dbo].[PrivateConversations];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.PrivateConversations', N'LastMessageSortKey') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PrivateConversations] DROP COLUMN [LastMessageSortKey];
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PrivateConversations_LastMessageAt'
                      AND object_id = OBJECT_ID(N'dbo.PrivateConversations'))
                BEGIN
                    CREATE INDEX [IX_PrivateConversations_LastMessageAt]
                        ON [dbo].[PrivateConversations] ([LastMessageAt] DESC);
                END
                """);
        }
    }
}
