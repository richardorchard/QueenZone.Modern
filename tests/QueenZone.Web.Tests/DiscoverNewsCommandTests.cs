using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class DiscoverNewsCommandTests
{
    [Fact]
    public void Parse_returns_null_for_missing_command()
    {
        Assert.Null(DiscoverNewsCommandOptions.Parse([]));
        Assert.Null(DiscoverNewsCommandOptions.Parse(["other-command"]));
    }

    [Fact]
    public void Parse_returns_null_for_unknown_flags()
    {
        Assert.Null(DiscoverNewsCommandOptions.Parse(["discover-news", "--unknown"]));
    }

    [Fact]
    public void Parse_reads_supported_flags()
    {
        var options = DiscoverNewsCommandOptions.Parse([
            "discover-news",
            "--fetch-only",
            "--seed-sources",
            "--dry-run",
            "--force"]);

        Assert.NotNull(options);
        Assert.True(options.SeedSources);
        Assert.True(options.DryRun);
        Assert.True(options.Force);
    }

    [Fact]
    public void Parse_reads_triage_flags()
    {
        var options = DiscoverNewsCommandOptions.Parse([
            "discover-news",
            "--triage-only",
            "--dry-run"]);

        Assert.NotNull(options);
        Assert.True(options.Triage);
        Assert.True(options.TriageOnly);
        Assert.True(options.DryRun);
    }

    [Fact]
    public void Parse_reads_draft_flags()
    {
        var options = DiscoverNewsCommandOptions.Parse([
            "discover-news",
            "--draft-only",
            "--force"]);

        Assert.NotNull(options);
        Assert.True(options.Draft);
        Assert.True(options.DraftOnly);
        Assert.True(options.Force);
    }
}
