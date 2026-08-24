using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds <c>SourceKey</c> to the <c>dbo.SearchDocument_Search</c> result set so JSON clients
/// can deep-link without parsing listing URLs (fan-performance rows share one index path).
/// </summary>
/// <remarks>
/// Procedure body source of truth: <c>docs/sql/010-search-document-full-text-search.sql</c>.
/// No EF model change — <c>SearchDocument.SourceKey</c> already exists.
/// </remarks>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260824120000_AddSearchDocumentSearchSourceKey")]
public partial class AddSearchDocumentSearchSourceKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
                    d.SourceKey,
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
}
