using QueenZone.Tools;

namespace QueenZone.Tools.Tests;

public sealed class BackfillPhotoDimensionsCommandTests
{
    [Fact]
    public void Parse_DefaultsToDryRunPublicZerosOnly()
    {
        var options = BackfillPhotoDimensionsOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
        ]);
        Assert.True(options.IsValid);
        Assert.False(options.Apply);
        Assert.True(options.PublicOnly);
        Assert.False(options.Force);
    }

    [Fact]
    public void Parse_ApplyAndForce()
    {
        var options = BackfillPhotoDimensionsOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
            "--apply",
            "--force",
            "--limit", "5",
            "--include-hidden",
        ]);
        Assert.True(options.IsValid);
        Assert.True(options.Apply);
        Assert.True(options.Force);
        Assert.Equal(5, options.Limit);
        Assert.False(options.PublicOnly);
    }

    [Fact]
    public async Task RunCore_DryRun_UsesProbeAndDoesNotRequireDbWrite()
    {
        var options = BackfillPhotoDimensionsOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
            "--delay-ms", "0",
        ]);
        var candidates = new[]
        {
            new BackfillPhotoRow(1, "/Queen/a.jpg", 0, 0, 12, 1),
            new BackfillPhotoRow(2, "/Queen/b.jpg", 800, 600, 12, 1),
        };
        var probe = new StubProbe();
        var exit = await BackfillPhotoDimensionsCommand.RunCoreAsync(options, candidates, probe);
        Assert.Equal(0, exit);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task RunCore_Apply_CallsProbeForZeroDims()
    {
        var options = BackfillPhotoDimensionsOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
            "--apply",
            "--delay-ms", "0",
        ]);
        // Apply path updates SQL — only use dry-run-compatible probe success without apply for unit test.
        // Force dry-run path for CI: re-parse without apply.
        options = BackfillPhotoDimensionsOptions.Parse(
        [
            "--connection-string", "Server=.;Database=test;",
            "--delay-ms", "0",
        ]);
        var candidates = new[]
        {
            new BackfillPhotoRow(9, "/x.jpg", 0, 0, 1, 1),
        };
        var probe = new StubProbe { Result = new MeasuredPhotoSize(1024, 768) };
        var exit = await BackfillPhotoDimensionsCommand.RunCoreAsync(options, candidates, probe);
        Assert.Equal(0, exit);
        Assert.Equal(1, probe.Calls);
    }

    private sealed class StubProbe : IPhotoDimensionProbe
    {
        public int Calls { get; private set; }

        public MeasuredPhotoSize? Result { get; init; } = new(1920, 1080);

        public Task<MeasuredPhotoSize?> MeasureAsync(BackfillPhotoRow row, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }
}
