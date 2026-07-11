using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Creates ForumPostAttachments for modern forum file uploads (issue #233).
/// Hand-written with [Migration] so EF discovers it without a Designer pair.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260711050000_AddForumPostAttachments")]
public partial class AddForumPostAttachments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ForumPostAttachments', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ForumPostAttachments
                (
                    Id uniqueidentifier NOT NULL,
                    PostId bigint NOT NULL,
                    LegacyPostId int NOT NULL,
                    OriginalFileName nvarchar(255) NOT NULL,
                    BlobPath nvarchar(512) NOT NULL,
                    ContainerName nvarchar(64) NOT NULL,
                    FileSizeBytes bigint NOT NULL,
                    MimeType nvarchar(100) NOT NULL,
                    UploadedAt datetimeoffset NOT NULL,
                    DownloadCount int NOT NULL CONSTRAINT DF_ForumPostAttachments_DownloadCount DEFAULT (0),
                    CONSTRAINT PK_ForumPostAttachments PRIMARY KEY (Id)
                );

                CREATE INDEX IX_ForumPostAttachments_LegacyPostId
                    ON dbo.ForumPostAttachments (LegacyPostId);

                CREATE INDEX IX_ForumPostAttachments_PostId
                    ON dbo.ForumPostAttachments (PostId);
            END

            -- Optional FK to imported modern posts when that table is present.
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.ForumPostAttachments', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_ForumPostAttachments_ModernForumPost_PostId')
            BEGIN
                ALTER TABLE dbo.ForumPostAttachments
                ADD CONSTRAINT FK_ForumPostAttachments_ModernForumPost_PostId
                    FOREIGN KEY (PostId) REFERENCES dbo.ModernForumPost (Id)
                    ON DELETE CASCADE;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ForumPostAttachments', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_ForumPostAttachments_ModernForumPost_PostId')
                BEGIN
                    ALTER TABLE dbo.ForumPostAttachments
                    DROP CONSTRAINT FK_ForumPostAttachments_ModernForumPost_PostId;
                END

                DROP TABLE dbo.ForumPostAttachments;
            END
            """);
    }
}
