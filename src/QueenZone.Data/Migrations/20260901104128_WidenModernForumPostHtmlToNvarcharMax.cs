using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Widens ModernForumPost.BodyHtml and SignatureHtml from varchar(8000) to nvarchar(max)
/// so emoji (and other non-Latin-1 characters) survive INSERT. The existing
/// <c>True ??</c> heart post is already replacement-corrupted and is not reconstructed.
/// </summary>
/// <remarks>
/// ModernForumPost is ExcludeFromMigrations — this is hand-written SQL, same pattern as
/// AddModernForumPostIsHidden, not a designer AlterColumn. nvarchar(8000) is illegal;
/// nvarchar(4000) would fail or truncate existing 8k Latin rows.
/// <para>
/// BodyHtml is full-text indexed. ALTER COLUMN on that type fails while the index
/// exists, so the index is dropped, the columns are altered, then the index is
/// recreated. DROP/CREATE FULLTEXT INDEX cannot share a user transaction, and a
/// transactional ALTER after a committed DROP left FTS missing when the ALTER
/// hit CommandTimeout=300. Every step uses <c>suppressTransaction: true</c>.
/// </para>
/// <para>
/// Retry-safe sequence: restore FTS if missing (CREATE on the current column type),
/// then DROP only if BodyHtml is still varchar, ALTER only if still varchar, CREATE
/// if missing. A re-run after DROP-committed + ALTER-timeout restores search first.
/// A re-run after a successful widen does not DROP FTS (columns are nvarchar).
/// </para>
/// <para>
/// Size-of-data on ~1M posts: expect a long lock and transaction-log growth. Apply
/// via CI/deploy <c>dotnet ef database update</c> (design-time factory uses
/// <see cref="QueenZoneSqlServerOptions.LongRunningCommandTimeoutSeconds"/>), not as
/// a silent app boot. Keep the table collation; do not specify COLLATE.
/// </para>
/// </remarks>
public partial class WidenModernForumPostHtmlToNvarcharMax : Migration
{
    private const string CreateBodyHtmlFullTextIndexSql = """
        IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost'))
           AND EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_ForumCatalog')
        BEGIN
            CREATE FULLTEXT INDEX ON dbo.ModernForumPost (BodyHtml)
                KEY INDEX PK_ModernForumPost
                ON FT_ForumCatalog
                WITH CHANGE_TRACKING AUTO;
        END
        """;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CreateBodyHtmlFullTextIndexSql, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost'))
               AND EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.ModernForumPost')
                      AND c.name = N'BodyHtml'
                      AND t.name = N'varchar')
            BEGIN
                DROP FULLTEXT INDEX ON dbo.ModernForumPost;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.ModernForumPost')
                      AND c.name = N'BodyHtml'
                      AND t.name = N'varchar')
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ALTER COLUMN BodyHtml nvarchar(max) NOT NULL;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.ModernForumPost')
                      AND c.name = N'SignatureHtml'
                      AND t.name = N'varchar')
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ALTER COLUMN SignatureHtml nvarchar(max) NULL;
                END;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql(CreateBodyHtmlFullTextIndexSql, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CreateBodyHtmlFullTextIndexSql, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost'))
               AND EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.ModernForumPost')
                      AND c.name = N'BodyHtml'
                      AND t.name = N'nvarchar')
            BEGIN
                DROP FULLTEXT INDEX ON dbo.ModernForumPost;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.ModernForumPost')
                      AND c.name = N'BodyHtml'
                      AND t.name = N'nvarchar')
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ALTER COLUMN BodyHtml varchar(8000) NOT NULL;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.ModernForumPost')
                      AND c.name = N'SignatureHtml'
                      AND t.name = N'nvarchar')
                BEGIN
                    ALTER TABLE dbo.ModernForumPost
                        ALTER COLUMN SignatureHtml varchar(8000) NULL;
                END;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql(CreateBodyHtmlFullTextIndexSql, suppressTransaction: true);
    }
}
