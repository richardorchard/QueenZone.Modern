using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds Azure SQL Full-Text Search infrastructure on the legacy <c>NEWS_T</c> table
/// and the <c>dbo.NEWS_T_SearchPublished</c> stored procedure used by
/// <see cref="QueenZone.Data.EfNewsRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// Procedure body source of truth for review: <c>docs/sql/008-news-full-text-search.sql</c>
/// (see <c>docs/sql/README.md</c>). Keep that file and this migration in sync.
/// </para>
/// <para>
/// FTS DDL (<c>CREATE FULLTEXT CATALOG</c>, <c>CREATE FULLTEXT INDEX</c>) cannot run inside a
/// transaction; all three steps use <c>suppressTransaction: true</c>.  Step 2 discovers the
/// existing primary-key or unique index at runtime via dynamic SQL so the migration does not
/// assume a specific PK constraint name on the legacy table.  If no unique index exists it
/// creates <c>UQ_NEWS_T_NEWS_ID_FTS</c> on <c>NEWS_ID</c>.
/// </para>
/// </remarks>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260729000000_AddNewsFullTextSearch")]
public partial class AddNewsFullTextSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_NewsCatalog')
                CREATE FULLTEXT CATALOG FT_NewsCatalog;
            """, suppressTransaction: true);

        // Use dynamic SQL to pick up any existing PK or unique index as the FTS key.
        // If none exists, create an explicit unique index on NEWS_ID.
        // Build the CREATE FULLTEXT INDEX string into @sql first; EXEC(N'...' + QUOTENAME(...) + N'...')
        // with multiline literals is rejected by the SQL Server parser.
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.NEWS_T', N'U'))
            BEGIN
                DECLARE @KeyIdx NVARCHAR(256);
                DECLARE @sql    NVARCHAR(MAX);

                SELECT TOP 1 @KeyIdx = name
                FROM   sys.indexes
                WHERE  object_id  = OBJECT_ID(N'dbo.NEWS_T', N'U')
                  AND  (is_primary_key = 1 OR is_unique = 1)
                  AND  is_disabled = 0
                ORDER BY is_primary_key DESC, index_id ASC;

                IF @KeyIdx IS NULL
                BEGIN
                    EXEC sp_executesql N'CREATE UNIQUE INDEX UQ_NEWS_T_NEWS_ID_FTS ON dbo.NEWS_T (NEWS_ID)';
                    SET @KeyIdx = N'UQ_NEWS_T_NEWS_ID_FTS';
                END

                SET @sql = N'CREATE FULLTEXT INDEX ON dbo.NEWS_T (TITLE, EXCERPT, ARTICLE) KEY INDEX '
                    + QUOTENAME(@KeyIdx)
                    + N' ON FT_NewsCatalog WITH CHANGE_TRACKING AUTO';
                EXEC sp_executesql @sql;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.NEWS_T_SearchPublished
                @Query        NVARCHAR(500),
                @Offset       INT,
                @PageSize     INT,
                @TotalRecords INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                -- Ranked FTS matches joined to deduplicated published rows.
                -- ROW_NUMBER guards against rare legacy duplicate NEWS_ID values.
                ;WITH FtsMatches AS (
                    SELECT ft.[KEY] AS NewsId, ft.[RANK] AS SearchRank
                    FROM   FREETEXTTABLE(dbo.NEWS_T, (TITLE, EXCERPT, ARTICLE), @Query) ft
                ),
                LatestPublished AS (
                    SELECT
                        n.NEWS_ID               AS Id,
                        ISNULL(n.TITLE, '')     AS Title,
                        ISNULL(n.EXCERPT, '')   AS Excerpt,
                        n.[DATE]                AS PublishedAt,
                        n.SOURCE_URL            AS SourceUrl,
                        CAST(1 AS bit)          AS IsPublished,
                        n.SLUG                  AS Slug,
                        fm.SearchRank,
                        ROW_NUMBER() OVER (PARTITION BY n.NEWS_ID ORDER BY n.[DATE] DESC, n.NEWS_ID DESC) AS RowNum
                    FROM   dbo.NEWS_T n
                    INNER JOIN FtsMatches fm ON fm.NewsId = n.NEWS_ID
                    WHERE  n.DISPLAY = 1
                )
                SELECT
                    Id,
                    Title,
                    Excerpt,
                    CAST(N'' AS nvarchar(max)) AS Body,
                    PublishedAt,
                    SourceUrl,
                    IsPublished,
                    Slug
                FROM LatestPublished
                WHERE RowNum = 1
                ORDER BY SearchRank DESC, PublishedAt DESC, Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                -- Count in a separate statement so @TotalRecords is set after the result set.
                ;WITH FtsMatchIds AS (
                    SELECT ft.[KEY] AS NewsId
                    FROM   FREETEXTTABLE(dbo.NEWS_T, (TITLE, EXCERPT, ARTICLE), @Query) ft
                ),
                LatestPublishedIds AS (
                    SELECT
                        n.NEWS_ID,
                        ROW_NUMBER() OVER (PARTITION BY n.NEWS_ID ORDER BY n.[DATE] DESC, n.NEWS_ID DESC) AS RowNum
                    FROM dbo.NEWS_T n
                    INNER JOIN FtsMatchIds fm ON fm.NewsId = n.NEWS_ID
                    WHERE  n.DISPLAY = 1
                )
                SELECT @TotalRecords = COUNT(*)
                FROM LatestPublishedIds
                WHERE RowNum = 1;
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.NEWS_T_SearchPublished;");

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.NEWS_T', N'U'))
                DROP FULLTEXT INDEX ON dbo.NEWS_T;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_NewsCatalog')
                DROP FULLTEXT CATALOG FT_NewsCatalog;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS UQ_NEWS_T_NEWS_ID_FTS ON dbo.NEWS_T;
            """);
    }
}
