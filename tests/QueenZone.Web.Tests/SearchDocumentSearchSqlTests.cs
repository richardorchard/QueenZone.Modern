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
