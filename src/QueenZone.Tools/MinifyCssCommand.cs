using NUglify;

namespace QueenZone.Tools;

/// <summary>
/// Minifies one or more CSS files, writing each result as a "*.min.css" sibling of its source file.
/// Used by QueenZone.Web's publish pipeline; see the "MinifyCss" MSBuild target in QueenZone.Web.csproj.
/// </summary>
internal static class MinifyCssCommand
{
    public static Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("minify-css requires at least one input CSS file path.");
            Console.Error.WriteLine("Usage: dotnet run --project src/QueenZone.Tools -- minify-css <path.css> [more paths...]");
            return Task.FromResult(2);
        }

        var hadError = false;

        foreach (var inputPath in args)
        {
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"CSS file was not found: {inputPath}");
                hadError = true;
                continue;
            }

            var source = File.ReadAllText(inputPath);
            var result = Uglify.Css(source);

            if (result.HasErrors)
            {
                Console.Error.WriteLine($"Failed to minify {inputPath}:");
                foreach (var error in result.Errors)
                {
                    Console.Error.WriteLine($"  {error}");
                }

                hadError = true;
                continue;
            }

            var directory = Path.GetDirectoryName(inputPath);
            var outputPath = Path.Combine(
                string.IsNullOrEmpty(directory) ? "." : directory,
                Path.GetFileNameWithoutExtension(inputPath) + ".min.css");

            File.WriteAllText(outputPath, result.Code);

            var originalBytes = new FileInfo(inputPath).Length;
            var minifiedBytes = new FileInfo(outputPath).Length;
            var savedPercent = originalBytes == 0 ? 0 : 100 - (minifiedBytes * 100.0 / originalBytes);
            Console.WriteLine($"{inputPath} -> {outputPath}: {originalBytes:N0}B -> {minifiedBytes:N0}B ({savedPercent:F0}% smaller)");
        }

        return Task.FromResult(hadError ? 1 : 0);
    }
}
