using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueenZone.NewsAgent;

public static class NewsDraftResultParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static NewsDraftStructuredResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Draft model returned empty structured output.");
        }

        var trimmed = json.Trim();
        var payload = trimmed.StartsWith('{')
            ? trimmed
            : ExtractJsonObject(trimmed);

        var parsed = JsonSerializer.Deserialize<ParsedDraftPayload>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Draft model returned invalid structured output.");

        if (string.IsNullOrWhiteSpace(parsed.Title))
        {
            throw new InvalidOperationException("Draft structured output is missing title.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Excerpt))
        {
            throw new InvalidOperationException("Draft structured output is missing excerpt.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Body))
        {
            throw new InvalidOperationException("Draft structured output is missing body.");
        }

        return new NewsDraftStructuredResult(
            parsed.Title.Trim(),
            string.IsNullOrWhiteSpace(parsed.Slug) ? null : parsed.Slug.Trim(),
            parsed.Excerpt.Trim(),
            parsed.Body.Trim(),
            parsed.RelatedEntities ?? [],
            parsed.SourceUrls ?? [],
            parsed.SourceNames ?? [],
            string.IsNullOrWhiteSpace(parsed.AttributionText) ? null : parsed.AttributionText.Trim(),
            string.IsNullOrWhiteSpace(parsed.ConfidenceNotes) ? null : parsed.ConfidenceNotes.Trim(),
            string.IsNullOrWhiteSpace(parsed.SourceNotes) ? null : parsed.SourceNotes.Trim(),
            parsed.SuggestedPublishAt,
            parsed.SecondarySourceWarning);
    }

    private static string ExtractJsonObject(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("Draft model did not return a JSON object.");
        }

        return value[start..(end + 1)];
    }

    private sealed record ParsedDraftPayload(
        string Title,
        string? Slug,
        string Excerpt,
        string Body,
        IReadOnlyList<string>? RelatedEntities,
        IReadOnlyList<string>? SourceUrls,
        IReadOnlyList<string>? SourceNames,
        string? AttributionText,
        string? ConfidenceNotes,
        string? SourceNotes,
        DateTime? SuggestedPublishAt,
        bool SecondarySourceWarning);
}
