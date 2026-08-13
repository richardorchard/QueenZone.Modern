using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace QueenZone.Web.Tests;

public sealed class QueenZoneEnvironmentsTests
{
    [Theory]
    [InlineData("Development", false)]
    [InlineData("Testing", true)]
    [InlineData("E2E", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    public void UsesTestAuth_matches_expected(string environmentName, bool expected)
    {
        var env = new FakeHostEnvironment(environmentName);
        Assert.Equal(expected, QueenZoneEnvironments.UsesTestAuth(env));
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Testing", true)]
    [InlineData("E2E", false)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    public void UsesInMemoryData_matches_expected(string environmentName, bool expected)
    {
        var env = new FakeHostEnvironment(environmentName);
        Assert.Equal(expected, QueenZoneEnvironments.UsesInMemoryData(env));
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Testing", true)]
    [InlineData("E2E", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    public void UsesInMemoryBlobStorage_matches_expected(string environmentName, bool expected)
    {
        var env = new FakeHostEnvironment(environmentName);
        Assert.Equal(expected, QueenZoneEnvironments.UsesInMemoryBlobStorage(env));
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Testing", true)]
    [InlineData("E2E", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    public void IsAutomatedTestHost_matches_expected(string environmentName, bool expected)
    {
        var env = new FakeHostEnvironment(environmentName);
        Assert.Equal(expected, QueenZoneEnvironments.IsAutomatedTestHost(env));
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Testing", false)]
    [InlineData("E2E", false)]
    [InlineData("Staging", true)]
    [InlineData("Preview", true)]
    [InlineData("Production", true)]
    public void IsProductionLike_matches_expected(string environmentName, bool expected)
    {
        var env = new FakeHostEnvironment(environmentName);
        Assert.Equal(expected, QueenZoneEnvironments.IsProductionLike(env));
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
