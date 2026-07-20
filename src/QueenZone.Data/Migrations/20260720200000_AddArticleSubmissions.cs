using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Creates community article submission storage. Must carry <see cref="MigrationAttribute"/>
/// so EF discovers it (this migration has no Designer companion).
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260720200000_AddArticleSubmissions")]
public partial class AddArticleSubmissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ArticleSubmissions', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ArticleSubmissions] (
                    [Id] uniqueidentifier NOT NULL,
                    [AuthorMemberId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(300) NOT NULL,
                    [Slug] nvarchar(300) NOT NULL,
                    [Excerpt] nvarchar(500) NULL,
                    [Body] nvarchar(max) NOT NULL,
                    [CoverImageBlobPath] nvarchar(512) NULL,
                    [Tags] nvarchar(500) NULL,
                    [Status] nvarchar(50) NOT NULL,
                    [SubmittedAt] datetimeoffset NULL,
                    [PublishedAt] datetimeoffset NULL,
                    [ReviewerEmail] nvarchar(256) NULL,
                    [ReviewNotes] nvarchar(1000) NULL,
                    [RejectionReason] nvarchar(1000) NULL,
                    CONSTRAINT [PK_ArticleSubmissions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ArticleSubmissions_MemberAccounts_AuthorMemberId]
                        FOREIGN KEY ([AuthorMemberId]) REFERENCES [dbo].[MemberAccounts] ([Id])
                );
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'UQ_ArticleSubmissions_Slug'
                  AND object_id = OBJECT_ID(N'dbo.ArticleSubmissions'))
            BEGIN
                CREATE UNIQUE INDEX [UQ_ArticleSubmissions_Slug]
                ON [dbo].[ArticleSubmissions] ([Slug])
                WHERE [Status] = N'Published';
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_ArticleSubmissions_Status_SubmittedAt'
                  AND object_id = OBJECT_ID(N'dbo.ArticleSubmissions'))
            BEGIN
                CREATE INDEX [IX_ArticleSubmissions_Status_SubmittedAt]
                ON [dbo].[ArticleSubmissions] ([Status], [SubmittedAt] DESC);
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_ArticleSubmissions_Author_SubmittedAt'
                  AND object_id = OBJECT_ID(N'dbo.ArticleSubmissions'))
            BEGIN
                CREATE INDEX [IX_ArticleSubmissions_Author_SubmittedAt]
                ON [dbo].[ArticleSubmissions] ([AuthorMemberId], [SubmittedAt] DESC);
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ArticleSubmissions', N'U') IS NOT NULL
            BEGIN
                DROP TABLE [dbo].[ArticleSubmissions];
            END
            """);
    }
}
