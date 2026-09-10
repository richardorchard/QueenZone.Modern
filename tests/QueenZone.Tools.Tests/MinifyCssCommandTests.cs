using QueenZone.Tools;

namespace QueenZone.Tools.Tests;

public sealed class MinifyCssCommandTests
{
    [Fact]
    public async Task RunAsync_ReturnsError_WhenNoPathsGiven()
    {
        var exitCode = await MinifyCssCommand.RunAsync([]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task RunAsync_ReturnsError_WhenInputFileDoesNotExist()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"qz-minify-missing-{Guid.NewGuid():N}.css");

        var exitCode = await MinifyCssCommand.RunAsync([missingPath]);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task RunAsync_WritesMinifiedSibling_ForValidCss()
    {
        var inputPath = WriteCssFile(
            """
            .qz-example {
              color: red; /* comment removed by minification */
              padding: 0 0 0 0;
            }
            """);
        var expectedOutputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + ".min.css");

        try
        {
            var exitCode = await MinifyCssCommand.RunAsync([inputPath]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutputPath));

            var minified = File.ReadAllText(expectedOutputPath);
            Assert.DoesNotContain("comment removed", minified, StringComparison.Ordinal);
            Assert.True(minified.Length < File.ReadAllText(inputPath).Length);
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(expectedOutputPath);
        }
    }

    [Fact]
    public async Task RunAsync_ReturnsError_ForInvalidCss()
    {
        var inputPath = WriteCssFile(
            """
            @media (min-width: 100px) {
              @keyframes broken {
                from { opacity: 0; }
              }
            }
            """);
        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + ".min.css");

        try
        {
            var exitCode = await MinifyCssCommand.RunAsync([inputPath]);

            Assert.Equal(1, exitCode);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    private static string WriteCssFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"qz-minify-{Guid.NewGuid():N}.css");
        File.WriteAllText(path, contents);
        return path;
    }
}
