using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds Azure SQL Full-Text Search infrastructure on the modern <c>SearchDocument</c> table
/// and the <c>dbo.SearchDocument_Search</c> stored procedure used by the unified whole-site
/// search page.
/// </summary>
/// <remarks>
/// Procedure body source of truth for review: <c>docs/sql/010-search-document-full-text-search.sql</c>
/// (see <c>docs/sql/README.md</c>). Keep that file and this migration in sync.
/// FTS DDL cannot run inside a transaction, so the catalog/index steps use
/// <c>suppressTransaction: true</c>, matching <c>20260729000000_AddNewsFullTextSearch</c>.
/// This migration has no model changes (SearchDocument's shape/indexes were added by
/// <c>20260804113427_AddSearchDocument</c>), so it carries no Designer companion.
/// </remarks>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260804113500_AddSearchDocumentFullTextSearch")]
public partial class AddSearchDocumentFullTextSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_SearchCatalog')
                CREATE FULLTEXT CATALOG FT_SearchCatalog;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.SearchDocument', N'U'))
                CREATE FULLTEXT INDEX ON dbo.SearchDocument (Title, Body)
                    KEY INDEX PK_SearchDocument ON FT_SearchCatalog WITH CHANGE_TRACKING AUTO;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.SearchDocument_Search
                @Query        NVARCHAR(500),
                @ContentType  NVARCHAR(50) = NULL,
                @Offset       INT,
                @PageSize     INT,
                @TotalRecords INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                ;WITH FtsMatches AS (
                    SELECT ft.[KEY] AS DocumentId, ft.[RANK] AS SearchRank
                    FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query) ft
                )
                SELECT
                    d.ContentType,
                    d.Title,
                    d.Summary,
                    d.Url,
                    d.PublishedAt,
                    d.ImageUrl,
                    d.Category,
                    d.AuthorDisplayName
                FROM   dbo.SearchDocument d
                INNER JOIN FtsMatches fm ON fm.DocumentId = d.Id
                WHERE  @ContentType IS NULL OR d.ContentType = @ContentType
                ORDER BY fm.SearchRank DESC, d.PublishedAt DESC, d.Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                -- Count in a separate statement so @TotalRecords is set after the result set.
                ;WITH FtsMatchIds AS (
                    SELECT ft.[KEY] AS DocumentId
                    FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query) ft
                )
                SELECT @TotalRecords = COUNT(*)
                FROM   dbo.SearchDocument d
                INNER JOIN FtsMatchIds fm ON fm.DocumentId = d.Id
                WHERE  @ContentType IS NULL OR d.ContentType = @ContentType;
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SearchDocument_Search;");

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.SearchDocument', N'U'))
                DROP FULLTEXT INDEX ON dbo.SearchDocument;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_SearchCatalog')
                DROP FULLTEXT CATALOG FT_SearchCatalog;
            """, suppressTransaction: true);
    }
}
