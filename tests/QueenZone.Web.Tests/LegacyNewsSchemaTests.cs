using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyNewsSchemaTests
{
    [Fact]
    public void BuildPublishedNewsCte_UsesSlugColumnWhenAvailable()
    {
        var cte = LegacyNewsSchema.BuildPublishedNewsCte(includeSlugColumn: true);

        Assert.Contains("SLUG AS Slug", cte);
        Assert.DoesNotContain("CAST(NULL AS nvarchar(200)) AS Slug", cte);
    }

    [Fact]
    public void BuildPublishedNewsCte_OmitsSlugColumnWhenUnavailable()
    {
        var cte = LegacyNewsSchema.BuildPublishedNewsCte(includeSlugColumn: false);

        Assert.Contains("CAST(NULL AS nvarchar(200)) AS Slug", cte);
        Assert.DoesNotContain("SLUG AS Slug", cte);
    }

    [Fact]
    public void BuildAdminLatestNewsSql_UsesFallbacksForUnavailableAdminColumns()
    {
        var sql = LegacyNewsSchema.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
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
        Assert.Contains("ISNULL(TYPE, 0) AS TYPE", sql);
        Assert.Contains("ISNULL(QUEEN_ONLINE, 0) AS QUEEN_ONLINE", sql);
        Assert.Contains("NEWS_ID", sql);
        Assert.DoesNotContain("AS NewsId", sql);
    }

    [Fact]
    public void BuildAdminLatestNewsSql_UsesAdminColumnsWhenAvailable()
    {
        var sql = LegacyNewsSchema.BuildAdminLatestNewsSql(new LegacyNewsSchema.NewsColumnAvailability
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
}
