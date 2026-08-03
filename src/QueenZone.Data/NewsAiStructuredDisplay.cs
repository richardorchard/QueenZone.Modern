using System.Text.Json;

namespace QueenZone.Data;

public static class NewsAiStructuredDisplay
{
    public static bool TryReadTriageSummary(string? structuredJson, out NewsTriageDisplaySummary? summary)
    {
        summary = null;
        if (string.IsNullOrWhiteSpace(structuredJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(structuredJson);
            var root = document.RootElement;
            var rationale = TryGetString(root, "rationale");
            if (string.IsNullOrWhiteSpace(rationale))
            {
                return false;
            }

            summary = new NewsTriageDisplaySummary(
                TryGetString(root, "verdict"),
                rationale.Trim(),
                ReadStringArray(root, "entities"),
                TryGetString(root, "review_notes"));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryReadPreservedQuotes(
        string? structuredJson,
        out IReadOnlyList<NewsDraftPreservedQuoteDisplay> quotes)
    {
        quotes = [];
        if (string.IsNullOrWhiteSpace(structuredJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(structuredJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("preserved_quotes", out var property)
                || property.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var parsed = new List<NewsDraftPreservedQuoteDisplay>();
            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var exactText = TryGetString(item, "exact_text");
                if (string.IsNullOrWhiteSpace(exactText))
                {
                    continue;
                }

                parsed.Add(new NewsDraftPreservedQuoteDisplay(
                    TryGetString(item, "speaker") ?? "Unknown",
                    exactText.Trim(),
                    TryGetString(item, "source_url") ?? string.Empty,
                    TryGetString(item, "source_context")));
            }

            quotes = parsed;
            return parsed.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }
}

public sealed record NewsTriageDisplaySummary(
    string? Verdict,
    string Rationale,
    IReadOnlyList<string> Entities,
    string? ReviewNotes);

public sealed record NewsDraftPreservedQuoteDisplay(
    string Speaker,
    string ExactText,
    string SourceUrl,
    string? SourceContext);
