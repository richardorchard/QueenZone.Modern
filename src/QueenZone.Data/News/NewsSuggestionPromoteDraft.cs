namespace QueenZone.Data;

public static class NewsSuggestionPromoteDraft
{
    public static AdminNewsDraft Build(NewsSuggestion suggestion)
    {
        var title = string.IsNullOrWhiteSpace(suggestion.Title)
            ? DeriveTitleFromUrl(suggestion.Url)
            : suggestion.Title.Trim();

        var excerpt = string.IsNullOrWhiteSpace(suggestion.Notes)
            ? $"Community-suggested news story from {suggestion.Url}"
            : suggestion.Notes.Trim();

        if (excerpt.Length > NewsValidation.MaxExcerptLength)
        {
            excerpt = excerpt[..NewsValidation.MaxExcerptLength];
        }

        var body = string.IsNullOrWhiteSpace(suggestion.Notes)
            ? string.Empty
            : suggestion.Notes.Trim();

        return new AdminNewsDraft(
            title,
            Slug: null,
            excerpt,
            body,
            DateTime.UtcNow.Date,
            suggestion.Url);
    }

    private static string DeriveTitleFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "Suggested news story";
        }

        var segment = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(segment))
        {
            return uri.Host;
        }

        segment = Uri.UnescapeDataString(segment)
            .Replace('-', ' ')
            .Replace('_', ' ');

        return segment.Length > NewsValidation.MaxTitleLength
            ? segment[..NewsValidation.MaxTitleLength]
            : segment;
    }
}
