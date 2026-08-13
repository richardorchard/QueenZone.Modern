namespace QueenZone.Data;

public static class NewsValidation
{
    public const int MaxTitleLength = 150;

    public const int MaxExcerptLength = 800;

    public const int MaxSourceUrlLength = 500;

    public static IReadOnlyList<string> ValidateDraft(AdminNewsDraft draft, bool slugInUse)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            errors.Add("Title is required.");
        }
        else if (draft.Title.Length > MaxTitleLength)
        {
            errors.Add($"Title must be {MaxTitleLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(draft.Excerpt))
        {
            errors.Add("Excerpt is required.");
        }
        else if (draft.Excerpt.Length > MaxExcerptLength)
        {
            errors.Add($"Excerpt must be {MaxExcerptLength} characters or fewer.");
        }

        if (draft.PublishedAt == default)
        {
            errors.Add("Publication date is required.");
        }

        if (draft.PublishedAt > DateTime.UtcNow.AddDays(1))
        {
            errors.Add("Publication date cannot be more than one day in the future.");
        }

        var resolvedSlug = NewsSlug.Resolve(draft.Title, draft.Slug);
        if (string.IsNullOrWhiteSpace(resolvedSlug))
        {
            errors.Add("Slug cannot be empty.");
        }

        if (slugInUse)
        {
            errors.Add("Slug is already in use by another article.");
        }

        if (!string.IsNullOrWhiteSpace(draft.SourceUrl) && draft.SourceUrl.Length > MaxSourceUrlLength)
        {
            errors.Add($"Source URL must be {MaxSourceUrlLength} characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(draft.SourceUrl) && !IsSafePublicUrl(draft.SourceUrl))
        {
            errors.Add("Source URL must be a safe http or https link.");
        }

        return errors;
    }

    public static bool IsSafePublicUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

}
