using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Tools;

return await ToolsApp.RunAsync(args);

internal static class ToolsApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "check-photos", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckPhotosCommand.RunAsync(args[1..]);
        }

        if (args.Length > 0 && string.Equals(args[0], "generate-photo-thumbs", StringComparison.OrdinalIgnoreCase))
        {
            return await GeneratePhotoThumbsCommand.RunAsync(args[1..]);
        }

        return await RunImportHistoryAsync(args);
    }

    private static async Task<int> RunImportHistoryAsync(string[] args)
    {
        var options = ImportOptions.Parse(args);
        if (!options.IsValid)
        {
            Console.Error.WriteLine(options.ErrorMessage);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  dotnet run --project src/QueenZone.Tools -- import-history --csv <path> --connection-string <connection-string>");
            Console.Error.WriteLine("  dotnet run --project src/QueenZone.Tools -- import-history --csv <path> --dry-run");
            Console.Error.WriteLine("  dotnet run --project src/QueenZone.Tools -- check-photos [options]");
            Console.Error.WriteLine("  dotnet run --project src/QueenZone.Tools -- generate-photo-thumbs [options]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Connection string can also be supplied with ConnectionStrings__QueenZoneLegacy.");
            return 2;
        }

        if (!File.Exists(options.CsvPath))
        {
            Console.Error.WriteLine($"CSV file was not found: {options.CsvPath}");
            return 2;
        }

        if (options.DryRun)
        {
            var rows = QueenHistoryCsvImporter.ReadRows(options.CsvPath);
            Console.WriteLine($"Rows read: {rows.Count}");
            Console.WriteLine("Dry run only. No database changes were made.");
            return 0;
        }

        var dbOptions = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(options.ConnectionString)
            .Options;

        await using var dbContext = new QueenZoneDbContext(dbOptions);
        var importer = new QueenHistoryCsvImporter(dbContext);
        var result = await importer.ImportAsync(options.CsvPath, DateTime.UtcNow);

        Console.WriteLine($"Rows read: {result.RowsRead}");
        Console.WriteLine($"Created: {result.Created}");
        Console.WriteLine($"Updated: {result.Updated}");
        Console.WriteLine($"Unchanged: {result.Unchanged}");
        return 0;
    }
}

internal sealed class ImportOptions
{
    private ImportOptions()
    {
    }

    public string CsvPath { get; private init; } = string.Empty;

    public string ConnectionString { get; private init; } = string.Empty;

    public bool IsValid { get; private init; }

    public bool DryRun { get; private init; }

    public string ErrorMessage { get; private init; } = string.Empty;

    public static ImportOptions Parse(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "import-history", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("Command is required.");
        }

        string? csvPath = null;
        string? connectionString = null;
        var dryRun = false;
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            if (string.Equals(arg, "--csv", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                csvPath = args[++index];
                continue;
            }

            if (string.Equals(arg, "--connection-string", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                connectionString = args[++index];
                continue;
            }

            if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
                continue;
            }

            return Invalid($"Unsupported or incomplete argument: {arg}");
        }

        connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy");
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            return Invalid("--csv is required.");
        }

        if (!dryRun && string.IsNullOrWhiteSpace(connectionString))
        {
            return Invalid("--connection-string or ConnectionStrings__QueenZoneLegacy is required.");
        }

        return new ImportOptions
        {
            CsvPath = csvPath,
            ConnectionString = connectionString ?? string.Empty,
            DryRun = dryRun,
            IsValid = true,
        };
    }

    private static ImportOptions Invalid(string message) =>
        new()
        {
            ErrorMessage = message,
            IsValid = false,
        };
}
