using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryFanPerformanceRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingPerformance()
    {
        var repository = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());

        var performance = await repository.GetByIdAsync(187);

        Assert.NotNull(performance);
        Assert.Equal("Reaching Out", performance.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        var repository = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());

        var performance = await repository.GetByIdAsync(999999);

        Assert.Null(performance);
    }
}
