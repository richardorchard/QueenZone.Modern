using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Caps <c>dbo.SearchDocument_Search</c> to one <c>FREETEXTTABLE</c> pass with
/// <c>top_n_by_rank</c> so common terms no longer exceed the 30-second command timeout.
/// </summary>
/// <remarks>
/// Procedure body source of truth: <c>docs/sql/010-search-document-full-text-search.sql</c>.
/// No EF model change.
/// </remarks>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260827143000_CapSearchDocumentSearchMatches")]
public partial class CapSearchDocumentSearchMatches : Migration
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
                @TotalRecords INT OUTPUT,
                @RankLimit    INT = 1000
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @MatchLimit INT = CASE
                    WHEN @RankLimit IS NULL OR @RankLimit < 1 THEN 1000
                    WHEN @RankLimit > 1000 THEN 1000
                    ELSE @RankLimit
                END;

                CREATE TABLE #Matches
                (
                    DocumentId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    SearchRank INT NOT NULL
                );

                INSERT INTO #Matches (DocumentId, SearchRank)
                SELECT ft.[KEY], ft.[RANK]
                FROM   FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query, @MatchLimit) ft;

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
                INNER JOIN #Matches fm ON fm.DocumentId = d.Id
                WHERE  @ContentType IS NULL OR d.ContentType = @ContentType
                ORDER BY fm.SearchRank DESC, d.PublishedAt DESC, d.Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                SELECT @TotalRecords = COUNT(*)
                FROM   dbo.SearchDocument d
                INNER JOIN #Matches fm ON fm.DocumentId = d.Id
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
}
