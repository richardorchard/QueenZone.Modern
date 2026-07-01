using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueenZone.NewsAgent;

public static class NewsTriageResultParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public static NewsTriageStructuredResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Triage model returned empty structured output.");
        }

        var trimmed = json.Trim();
        var payload = trimmed.StartsWith('{')
            ? trimmed
            : ExtractJsonObject(trimmed);

        var result = JsonSerializer.Deserialize<NewsTriageStructuredResult>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Triage model returned invalid structured output.");

        if (string.IsNullOrWhiteSpace(result.Rationale))
        {
            throw new InvalidOperationException("Triage structured output is missing rationale.");
        }

        return result with
        {
            RelevanceScore = ClampScore(result.RelevanceScore),
            ConfidenceScore = ClampScore(result.ConfidenceScore),
            Entities = result.Entities ?? [],
            ReviewNotes = string.IsNullOrWhiteSpace(result.ReviewNotes) ? null : result.ReviewNotes.Trim()
        };
    }

    private static string ExtractJsonObject(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("Triage model did not return a JSON object.");
        }

        return value[start..(end + 1)];
    }

    private static decimal ClampScore(decimal score) => Math.Clamp(score, 0m, 1m);
}
