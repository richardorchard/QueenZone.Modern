namespace QueenZone.Data;

/// <summary>
/// Small parsing helpers shared by the CSV importers (<see cref="QueenHistoryCsvImporter"/>,
/// <see cref="QuoteCsvImporter"/>, <see cref="TriviaFactCsvImporter"/>).
/// </summary>
internal static class CsvImportRowParsing
{
    public static string Required(string? value, int rowNumber, string column)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Row {rowNumber} {column} is required.");
        }

        return value.Trim();
    }

    public static TEnum ParseEnum<TEnum>(string value, int rowNumber, string column)
        where TEnum : struct =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Row {rowNumber} {column} has unsupported value '{value}'.");

    public static string BuildSourceKey<TEnum>(TEnum sourceType, string sourceKey)
        where TEnum : struct, Enum =>
        $"{sourceType}:{sourceKey}";
}
