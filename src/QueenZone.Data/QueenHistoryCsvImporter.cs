using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class QueenHistoryCsvImporter(QueenZoneDbContext dbContext)
{
    private static readonly string[] ExpectedHeaders =
    [
        "Title",
        "Summary",
        "EventDate",
        "DatePrecision",
        "Category",
        "Importance",
        "SourceType",
        "SourceKey",
        "SourceUrl",
    ];

    public async Task<QueenHistoryCsvImportResult> ImportAsync(
        string csvPath,
        DateTime importedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = ReadRows(csvPath);
        var sourceKeys = rows.Select(row => row.SourceKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sourceTypes = rows.Select(row => row.SourceType).Distinct().ToList();
        var existing = await dbContext.QueenHistoryEvents
            .Where(item => sourceTypes.Contains(item.SourceType) && sourceKeys.Contains(item.SourceKey))
            .ToListAsync(cancellationToken);
        var existingBySource = existing.ToDictionary(
            item => BuildSourceKey(item.SourceType, item.SourceKey),
            StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var row in rows)
        {
            if (existingBySource.TryGetValue(BuildSourceKey(row.SourceType, row.SourceKey), out var entity))
            {
                if (Apply(entity, row, importedAtUtc, isNew: false))
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            entity = new QueenHistoryEventEntity
            {
                CreatedAt = importedAtUtc,
            };
            Apply(entity, row, importedAtUtc, isNew: true);
            dbContext.QueenHistoryEvents.Add(entity);
            existingBySource[BuildSourceKey(row.SourceType, row.SourceKey)] = entity;
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new QueenHistoryCsvImportResult(rows.Count, created, updated, unchanged);
    }

    public static IReadOnlyList<QueenHistoryCsvImportRow> ReadRows(string csvPath)
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

        var rows = new List<QueenHistoryCsvImportRow>();
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

    private static QueenHistoryCsvImportRow ParseRow(string[] fields, int rowNumber)
    {
        var title = Required(fields[0], rowNumber, "Title");
        var summary = Required(fields[1], rowNumber, "Summary");
        var precision = ParseEnum<QueenHistoryDatePrecision>(fields[3], rowNumber, "DatePrecision");
        var category = ParseEnum<QueenHistoryEventCategory>(fields[4], rowNumber, "Category");
        var sourceType = ParseEnum<QueenHistoryEventSourceType>(fields[6], rowNumber, "SourceType");
        var sourceKey = Required(fields[7], rowNumber, "SourceKey");
        var sourceUrl = string.IsNullOrWhiteSpace(fields[8]) ? null : fields[8].Trim();

        if (title.Length > 200)
        {
            throw new InvalidOperationException($"Row {rowNumber} Title must be 200 characters or fewer.");
        }

        if (summary.Length > 1000)
        {
            throw new InvalidOperationException($"Row {rowNumber} Summary must be 1000 characters or fewer.");
        }

        if (sourceKey.Length > 200)
        {
            throw new InvalidOperationException($"Row {rowNumber} SourceKey must be 200 characters or fewer.");
        }

        if (sourceUrl is not null && sourceUrl.Length > 2000)
        {
            throw new InvalidOperationException($"Row {rowNumber} SourceUrl must be 2000 characters or fewer.");
        }

        if (sourceUrl is not null && !NewsValidation.IsSafePublicUrl(sourceUrl))
        {
            throw new InvalidOperationException($"Row {rowNumber} SourceUrl must be a safe http or https URL.");
        }

        if (!int.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var importance)
            || importance is < 0 or > 100)
        {
            throw new InvalidOperationException($"Row {rowNumber} Importance must be an integer from 0 to 100.");
        }

        return new QueenHistoryCsvImportRow(
            title,
            summary,
            ParseEventDate(fields[2], precision, rowNumber),
            precision,
            category,
            importance,
            sourceType,
            sourceKey,
            sourceUrl);
    }

    private static string BuildSourceKey(QueenHistoryEventSourceType sourceType, string sourceKey) =>
        $"{sourceType}:{sourceKey}";

    private static bool Apply(
        QueenHistoryEventEntity entity,
        QueenHistoryCsvImportRow row,
        DateTime importedAtUtc,
        bool isNew)
    {
        var changed = isNew
            || entity.Title != row.Title
            || entity.Summary != row.Summary
            || entity.EventDate != row.EventDate
            || entity.DatePrecision != row.DatePrecision
            || entity.Category != row.Category
            || entity.Importance != row.Importance
            || entity.SourceType != row.SourceType
            || entity.SourceKey != row.SourceKey
            || entity.SourceUrl != row.SourceUrl
            || !entity.IsPublished;

        if (!changed)
        {
            return false;
        }

        entity.Title = row.Title;
        entity.Summary = row.Summary;
        entity.EventDate = row.EventDate;
        entity.DatePrecision = row.DatePrecision;
        entity.Category = row.Category;
        entity.Importance = row.Importance;
        entity.SourceType = row.SourceType;
        entity.SourceKey = row.SourceKey;
        entity.SourceUrl = row.SourceUrl;
        entity.VerifiedAt = importedAtUtc;
        entity.IsPublished = true;
        entity.UpdatedAt = importedAtUtc;
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

    private static DateTime ParseEventDate(string value, QueenHistoryDatePrecision precision, int rowNumber)
    {
        string[] formats = precision switch
        {
            QueenHistoryDatePrecision.ExactDate => ["yyyy-MM-dd"],
            QueenHistoryDatePrecision.MonthYear => ["yyyy-MM"],
            QueenHistoryDatePrecision.YearOnly => ["yyyy"],
            _ => throw new InvalidOperationException($"Row {rowNumber} DatePrecision is unsupported."),
        };

        if (!DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed))
        {
            throw new InvalidOperationException($"Row {rowNumber} EventDate is invalid for {precision}.");
        }

        return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
    }
}
