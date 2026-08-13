namespace QueenZone.Data;

public static class BiographyValidation
{
    public const int MaxTitleLength = 50;

    public const int MaxSummaryLength = 400;

    public static IReadOnlyList<string> ValidateDraft(AdminBiographyDraft draft)
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

        if (draft.Summary.Length > MaxSummaryLength)
        {
            errors.Add($"Summary must be {MaxSummaryLength} characters or fewer.");
        }

        if (IsEmptyBody(draft.Body))
        {
            errors.Add("Chapter body is required.");
        }

        if (draft.DisplaySequence == 0)
        {
            errors.Add("Display sequence must be between 1 and 255.");
        }

        return errors;
    }

    internal static bool IsEmptyBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return true;
        }

        var trimmed = body.Trim();
        return trimmed is "<p><br></p>" or "<p></p>" or "<br>" or "<p><br/></p>" or "<p><br /></p>";
    }
}
