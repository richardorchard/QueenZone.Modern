using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace QueenZone.Web.Tests;

public sealed class QueenZoneDevelopmentHostTests
{
    [Theory]
    [InlineData("testhost", true)]
    [InlineData("testhost.dll", true)]
    [InlineData("ReSharperTestRunner", true)]
    [InlineData("QueenZone.Web", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsTestProcess_detects_vstest_hosts(string? assemblyName, bool expected)
    {
        Assert.Equal(expected, QueenZoneDevelopmentHost.IsTestProcess(assemblyName));
    }

    [Fact]
    public void ShouldLoadLocalSettings_only_for_interactive_development()
    {
        var config = new ConfigurationBuilder().Build();
        Assert.True(QueenZoneDevelopmentHost.ShouldLoadLocalSettings(
            new FakeHostEnvironment("Development"),
            config,
            "QueenZone.Web"));
        Assert.False(QueenZoneDevelopmentHost.ShouldLoadLocalSettings(
            new FakeHostEnvironment("Development"),
            config,
            "testhost"));
        Assert.False(QueenZoneDevelopmentHost.ShouldLoadLocalSettings(
            new FakeHostEnvironment("Testing"),
            config,
            "QueenZone.Web"));

        var skipped = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [QueenZoneDevelopmentHost.SkipLocalSettingsKey] = "true",
            })
            .Build();
        Assert.False(QueenZoneDevelopmentHost.ShouldLoadLocalSettings(
            new FakeHostEnvironment("Development"),
            skipped,
            "QueenZone.Web"));
    }

    [Fact]
    public void NeutralizeIncompleteAnalytics_clears_a_split_pair()
    {
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analytics:GoogleAnalyticsPropertyId"] = "123456",
            });

        QueenZoneDevelopmentHost.NeutralizeIncompleteAnalytics(builder);

        var config = builder.Build();
        Assert.True(string.IsNullOrWhiteSpace(config["Analytics:GoogleAnalyticsPropertyId"]));
        Assert.True(string.IsNullOrWhiteSpace(config["Analytics:GoogleAnalyticsServiceAccountJson"]));
    }

    [Fact]
    public void NeutralizeIncompleteAnalytics_leaves_complete_or_empty_pairs()
    {
        var empty = new ConfigurationBuilder();
        QueenZoneDevelopmentHost.NeutralizeIncompleteAnalytics(empty);
        Assert.True(string.IsNullOrWhiteSpace(empty.Build()["Analytics:GoogleAnalyticsPropertyId"]));

        var complete = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analytics:GoogleAnalyticsPropertyId"] = "123456",
                ["Analytics:GoogleAnalyticsServiceAccountJson"] = "{}",
            });
        QueenZoneDevelopmentHost.NeutralizeIncompleteAnalytics(complete);
        var config = complete.Build();
        Assert.Equal("123456", config["Analytics:GoogleAnalyticsPropertyId"]);
        Assert.Equal("{}", config["Analytics:GoogleAnalyticsServiceAccountJson"]);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
