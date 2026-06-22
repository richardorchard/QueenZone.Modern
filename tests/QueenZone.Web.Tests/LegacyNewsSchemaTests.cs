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
}