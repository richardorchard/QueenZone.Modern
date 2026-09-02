using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SearchDocumentSearchSqlTests
{
    [Fact]
    public void Rank_cap_matches_the_sql_source_of_truth()
    {
        Assert.Equal(1000, SiteSearchLimits.MaxRankedMatches);
        Assert.Contains(
            "@RankLimit    INT = 1000",
            ReadSqlSourceOfTruth(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Source_of_truth_runs_one_capped_freetext_pass()
    {
        var sql = ReadSqlSourceOfTruth();
        var freetextCount = CountOccurrences(sql, "FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query, @MatchLimit)");

        Assert.Equal(1, freetextCount);
        Assert.Contains("CREATE TABLE #Matches", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query)",
            sql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SearchDocument_fts_uses_auto_change_tracking_not_a_sync_rebuild()
    {
        var migration = ReadRepoFile(
            Path.Combine("src", "QueenZone.Data", "Migrations", "20260804113500_AddSearchDocumentFullTextSearch.cs"));

        Assert.Contains("WITH CHANGE_TRACKING AUTO", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("CHANGE_TRACKING OFF", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("CHANGE_TRACKING MANUAL", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("START FULL POPULATION", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("START UPDATE POPULATION", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Article_search_sync_is_single_row_upsert_not_a_content_type_replace()
    {
        var upsert = ReadRepoFile(
            Path.Combine("src", "QueenZone.Data", "Repositories", "EfSearchIndexService.cs"));
        var status = ReadRepoFile(
            Path.Combine("src", "QueenZone.Web", "Pages", "Admin", "Articles", "Status.cshtml.cs"));
        var action = ReadRepoFile(
            Path.Combine("src", "QueenZone.Web", "Pages", "Admin", "Articles", "Action.cshtml.cs"));
        var upsertMethod = upsert[upsert.IndexOf("public async Task UpsertAsync", StringComparison.Ordinal)..];
        upsertMethod = upsertMethod[..upsertMethod.IndexOf("public async Task RemoveAsync", StringComparison.Ordinal)];

        Assert.Contains("searchIndexService.UpsertAsync", status, StringComparison.Ordinal);
        Assert.Contains("searchIndexService.UpsertAsync", action, StringComparison.Ordinal);
        Assert.DoesNotContain("ReplaceContentTypeAsync", status, StringComparison.Ordinal);
        Assert.DoesNotContain("ReplaceContentTypeAsync", action, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransactionAsync", upsertMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("FULLTEXT", upsertMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("POPULATION", upsertMethod, StringComparison.Ordinal);
        Assert.Equal(30, QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
        Assert.Equal(1000, SiteSearchLimits.MaxRankedMatches);
    }

    [Fact]
    public void Migration_embeds_the_capped_procedure()
    {
        var migration = ReadRepoFile(
            Path.Combine("src", "QueenZone.Data", "Migrations", "20260827143000_CapSearchDocumentSearchMatches.cs"));

        Assert.Contains("@RankLimit    INT = 1000", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE #Matches", migration, StringComparison.Ordinal);
        Assert.Contains(
            "FREETEXTTABLE(dbo.SearchDocument, (Title, Body), @Query, @MatchLimit)",
            migration,
            StringComparison.Ordinal);
        Assert.Contains("SiteSearchLimits.MaxRankedMatches", ReadRepoFile(
            Path.Combine("src", "QueenZone.Data", "Repositories", "EfSiteSearchService.cs")));
    }

    private static string ReadSqlSourceOfTruth() =>
        ReadRepoFile(Path.Combine("docs", "sql", "010-search-document-full-text-search.sql"));

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath);
        Assert.True(File.Exists(path), $"Expected {relativePath} at {path}");
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "QueenZone.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find QueenZone.sln above the test output directory.");
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
