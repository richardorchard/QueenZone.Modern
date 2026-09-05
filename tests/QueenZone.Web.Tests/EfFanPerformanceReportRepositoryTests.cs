using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfFanPerformanceReportRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfFanPerformanceReportRepository repository;
    private readonly Guid reporterId = Guid.NewGuid();

    public EfFanPerformanceReportRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();

        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = reporterId,
            Email = "fp-report-ef@example.com",
            NormalizedEmail = "FP-REPORT-EF@EXAMPLE.COM",
            DisplayName = "EF Reporter",
            CreatedAt = DateTime.UtcNow,
        });
        dbContext.SaveChanges();

        repository = new EfFanPerformanceReportRepository(dbContext);
    }

    [Fact]
    public async Task CreateAsync_IsIdempotent_ForOneOpenReportPerMemberAndStage()
    {
        var first = await repository.CreateAsync(NewReport(187, "Rights issue"));
        var second = await repository.CreateAsync(NewReport(187, "Still a rights issue"));

        Assert.True(first.Succeeded);
        Assert.False(first.AlreadyReported);
        Assert.True(second.Succeeded);
        Assert.True(second.AlreadyReported);
        Assert.Equal(first.ReportId, second.ReportId);
        Assert.Equal(1, await repository.CountOpenAsync());
    }

    [Fact]
    public async Task CreateAsync_AllowsNewOpenReport_AfterPreviousWasResolved()
    {
        var first = await repository.CreateAsync(NewReport(186, "First report"));
        await repository.UpdateStatusAsync(first.ReportId!.Value, FanPerformanceReportStatus.Resolved, "admin@test.local");

        var second = await repository.CreateAsync(NewReport(186, "New report after resolve"));

        Assert.False(second.AlreadyReported);
        Assert.NotEqual(first.ReportId, second.ReportId);
        Assert.Equal(1, await repository.CountOpenAsync());
    }

    [Fact]
    public async Task ListAsync_ReturnsOpenFirst_AndGetByIdIncludesSnapshots()
    {
        var open = await repository.CreateAsync(NewReport(173, "Open reason", "Hammer to Fall", "Sonic Snafu"));
        var resolved = await repository.CreateAsync(NewReport(176, "Resolved reason"));
        await repository.UpdateStatusAsync(resolved.ReportId!.Value, FanPerformanceReportStatus.Dismissed, "admin@test.local");

        var page = await repository.ListAsync("all", 1, 50);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(open.ReportId, page.Items[0].Id);
        Assert.Equal(FanPerformanceReportStatus.Open, page.Items[0].Status);

        var loaded = await repository.GetByIdAsync(open.ReportId!.Value);
        Assert.Equal("EF Reporter", loaded!.ReporterDisplayName);
        Assert.Equal("Hammer to Fall", loaded.TitleSnapshot);
        Assert.Equal("Sonic Snafu", loaded.PerformedBySnapshot);
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    public ValueTask DisposeAsync()
    {
        dbContext.Dispose();
        connection.Dispose();
        return ValueTask.CompletedTask;
    }

    private NewFanPerformanceReport NewReport(
        int stageId,
        string reason,
        string? title = "Reaching Out",
        string? performedBy = "Mike Ryde") =>
        new(stageId, reporterId, reason, title, performedBy);
}
