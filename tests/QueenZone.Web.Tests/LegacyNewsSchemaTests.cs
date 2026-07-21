using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyNewsSchemaTests
{
    [Fact]
    public void BuildPublishedNewsCte_UsesSlugColumnWhenAvailable()
    {
        var cte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlugColumn: true);

        Assert.Contains("SLUG AS Slug", cte);
        Assert.DoesNotContain("CAST(NULL AS nvarchar(200)) AS Slug", cte);
    }

    [Fact]
    public void BuildPublishedNewsCte_OmitsSlugColumnWhenUnavailable()
    {
        var cte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlugColumn: false);

        Assert.Contains("CAST(NULL AS nvarchar(200)) AS Slug", cte);
        Assert.DoesNotContain("SLUG AS Slug", cte);
    }

    [Fact]
    public void BuildAdminLatestNewsSql_UsesFallbacksForUnavailableAdminColumns()
    {
        var sql = PublishedNewsQuery.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
        {
            HasSourceUrlColumn = true,
            HasSlugColumn = false,
            HasCreatedAtColumn = false,
            HasUpdatedAtColumn = false,
            HasEditorEmailColumn = false
        });

        Assert.Contains("SOURCE_URL", sql);
        Assert.Contains("CAST(NULL AS nvarchar(200)) AS SLUG", sql);
        Assert.Contains("CAST(NULL AS datetime2) AS CREATED_AT", sql);
        Assert.Contains("CAST(NULL AS datetime2) AS UPDATED_AT", sql);
        Assert.Contains("CAST(NULL AS nvarchar(256)) AS EDITOR_EMAIL", sql);
        Assert.Contains("CAST(ISNULL(TYPE, 0) AS int) AS TYPE", sql);
        Assert.Contains("CAST(ISNULL(QUEEN_ONLINE, 0) AS int) AS QUEEN_ONLINE", sql);
        Assert.Contains($"CASE WHEN {PublishedNewsQuery.PublishedFilter} THEN 1 ELSE 0 END AS DISPLAY", sql);
        Assert.DoesNotContain($"CAST(CASE WHEN {PublishedNewsQuery.PublishedFilter} THEN 1 ELSE 0 END AS bit) AS DISPLAY", sql);
        Assert.Contains("NEWS_ID", sql);
        Assert.Contains("[DATE] AS PublishedAt", sql);
        Assert.Contains("NEWS_ID AS NewsId", sql);
    }

    [Fact]
    public void BuildAdminLatestNewsSql_UsesAdminColumnsWhenAvailable()
    {
        var sql = PublishedNewsQuery.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
        {
            HasSourceUrlColumn = true,
            HasSlugColumn = true,
            HasCreatedAtColumn = true,
            HasUpdatedAtColumn = true,
            HasEditorEmailColumn = true
        });

        Assert.Contains("SLUG", sql);
        Assert.Contains("CREATED_AT", sql);
        Assert.Contains("UPDATED_AT", sql);
        Assert.Contains("EDITOR_EMAIL", sql);
        Assert.DoesNotContain("CAST(NULL AS nvarchar(200)) AS SLUG", sql);
        Assert.DoesNotContain("CAST(NULL AS datetime2) AS CREATED_AT", sql);
    }

    [Fact]
    public void BuildAdminLatestNewsSql_UsesSourceUrlFallbackMatchingValidationLimit()
    {
        var sql = PublishedNewsQuery.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
        {
            HasSourceUrlColumn = false,
            HasSlugColumn = true,
            HasCreatedAtColumn = true,
            HasUpdatedAtColumn = true,
            HasEditorEmailColumn = true
        });

        Assert.Contains($"CAST(NULL AS varchar({NewsValidation.MaxSourceUrlLength})) AS SOURCE_URL", sql);
    }

    [Fact]
    public void PublicAndAdminSql_ShareLatestRowAndPublishedRules()
    {
        var publicCte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlugColumn: true);
        var adminSql = PublishedNewsQuery.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
        {
            HasSourceUrlColumn = true,
            HasSlugColumn = true,
            HasCreatedAtColumn = true,
            HasUpdatedAtColumn = true,
            HasEditorEmailColumn = true
        });
        var adminCountSql = PublishedNewsQuery.BuildAdminLatestNewsCountSql(new LegacyNewsSchema.NewsColumnAvailability
        {
            HasSourceUrlColumn = true,
            HasSlugColumn = true,
            HasCreatedAtColumn = true,
            HasUpdatedAtColumn = true,
            HasEditorEmailColumn = true
        });

        Assert.Contains(PublishedNewsQuery.LatestRowNumberExpression, publicCte);
        Assert.Contains(PublishedNewsQuery.LatestRowNumberExpression, adminSql);
        Assert.Contains(PublishedNewsQuery.LatestRowNumberExpression, adminCountSql);
        Assert.Contains(PublishedNewsQuery.PublishedFilter, publicCte);
        Assert.Contains(PublishedNewsQuery.LatestRowFilter, adminSql);
        Assert.Contains(PublishedNewsQuery.LatestRowFilter, adminCountSql);

        // Compatibility wrappers still delegate to the same body.
        Assert.Equal(publicCte, LegacyNewsSchema.BuildPublishedNewsCte(includeSlugColumn: true));
        Assert.Equal(adminSql, LegacyNewsSchema.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
        {
            HasSourceUrlColumn = true,
            HasSlugColumn = true,
            HasCreatedAtColumn = true,
            HasUpdatedAtColumn = true,
            HasEditorEmailColumn = true
        }));
    }
}
