using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class QuoteCsvImporter(QueenZoneDbContext dbContext)
{
    private static readonly string[] ExpectedHeaders =
    [
        "Text",
        "WhoSaid",
        "Context",
        "SourceType",
        "SourceKey",
    ];

    public async Task<QuoteCsvImportResult> ImportAsync(
        string csvPath,
        DateTime importedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = ReadRows(csvPath);
        var sourceKeys = rows.Select(row => row.SourceKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sourceTypes = rows.Select(row => row.SourceType).Distinct().ToList();
        var existing = await dbContext.Quotes
            .Where(item => item.SourceType != null
                && item.SourceKey != null
                && sourceTypes.Contains(item.SourceType.Value)
                && sourceKeys.Contains(item.SourceKey))
            .ToListAsync(cancellationToken);
        var existingBySource = existing.ToDictionary(
            item => BuildSourceKey(item.SourceType!.Value, item.SourceKey!),
            StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var row in rows)
        {
            if (existingBySource.TryGetValue(BuildSourceKey(row.SourceType, row.SourceKey), out var entity))
            {
                if (Apply(entity, row, isNew: false))
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            entity = new QuoteEntity
            {
                CreatedAt = importedAtUtc,
            };
            Apply(entity, row, isNew: true);
            dbContext.Quotes.Add(entity);
            existingBySource[BuildSourceKey(row.SourceType, row.SourceKey)] = entity;
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new QuoteCsvImportResult(rows.Count, created, updated, unchanged);
    }

    public static IReadOnlyList<QuoteCsvImportRow> ReadRows(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            throw new ArgumentException("CSV path is required.", nameof(csvPath));
        }

        using var parser = new TextFieldParser(csvPath);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;

        var headers = parser.ReadFields()
            ?? throw new InvalidOperationException("CSV file is empty.");
        if (!headers.SequenceEqual(ExpectedHeaders, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"CSV header must be: {string.Join(",", ExpectedHeaders)}");
        }

        var rows = new List<QuoteCsvImportRow>();
        var rowNumber = 1;
        while (!parser.EndOfData)
        {
            rowNumber++;
            var fields = parser.ReadFields();
            if (fields is null || fields.Length == 0 || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Length != ExpectedHeaders.Length)
            {
                throw new InvalidOperationException($"Row {rowNumber} has {fields.Length} columns; expected {ExpectedHeaders.Length}.");
            }

            rows.Add(ParseRow(fields, rowNumber));
        }

        return rows;
    }

    private static QuoteCsvImportRow ParseRow(string[] fields, int rowNumber)
    {
        var text = Required(fields[0], rowNumber, "Text");
        var whoSaid = Required(fields[1], rowNumber, "WhoSaid");
        var context = string.IsNullOrWhiteSpace(fields[2]) ? null : fields[2].Trim();
        var sourceType = ParseEnum<QuoteSourceType>(fields[3], rowNumber, "SourceType");
        var sourceKey = Required(fields[4], rowNumber, "SourceKey");

        if (text.Length > QuoteValidation.MaxTextLength)
        {
            throw new InvalidOperationException($"Row {rowNumber} Text must be {QuoteValidation.MaxTextLength} characters or fewer.");
        }

        if (whoSaid.Length > QuoteValidation.MaxWhoSaidLength)
        {
            throw new InvalidOperationException($"Row {rowNumber} WhoSaid must be {QuoteValidation.MaxWhoSaidLength} characters or fewer.");
        }

        if (context is not null && context.Length > QuoteValidation.MaxContextLength)
        {
            throw new InvalidOperationException($"Row {rowNumber} Context must be {QuoteValidation.MaxContextLength} characters or fewer.");
        }

        if (sourceKey.Length > 200)
        {
            throw new InvalidOperationException($"Row {rowNumber} SourceKey must be 200 characters or fewer.");
        }

        return new QuoteCsvImportRow(text, whoSaid, context, sourceType, sourceKey);
    }

    private static string BuildSourceKey(QuoteSourceType sourceType, string sourceKey) =>
        $"{sourceType}:{sourceKey}";

    private static bool Apply(
        QuoteEntity entity,
        QuoteCsvImportRow row,
        bool isNew)
    {
        var changed = isNew
            || entity.Text != row.Text
            || entity.WhoSaid != row.WhoSaid
            || entity.Context != row.Context
            || entity.SourceType != row.SourceType
            || entity.SourceKey != row.SourceKey
            || !entity.IsPublished;

        if (!changed)
        {
            return false;
        }

        entity.Text = row.Text;
        entity.WhoSaid = row.WhoSaid;
        entity.Context = row.Context;
        entity.SourceType = row.SourceType;
        entity.SourceKey = row.SourceKey;
        entity.IsPublished = true;
        return true;
    }

    private static string Required(string? value, int rowNumber, string column)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Row {rowNumber} {column} is required.");
        }

        return value.Trim();
    }

    private static TEnum ParseEnum<TEnum>(string value, int rowNumber, string column)
        where TEnum : struct =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Row {rowNumber} {column} has unsupported value '{value}'.");
}
