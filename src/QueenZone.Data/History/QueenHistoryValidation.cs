namespace QueenZone.Data;

public static class QueenHistoryValidation
{
    public const int MaxTitleLength = 200;

    public const int MaxSummaryLength = 1000;

    public const int MaxSourceUrlLength = 2000;

    public const int MinImportance = 0;

    public const int MaxImportance = 100;

    public static IReadOnlyList<string> ValidateDraft(AdminQueenHistoryDraft draft)
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

        if (string.IsNullOrWhiteSpace(draft.Summary))
        {
            errors.Add("Summary is required.");
        }
        else if (draft.Summary.Length > MaxSummaryLength)
        {
            errors.Add($"Summary must be {MaxSummaryLength} characters or fewer.");
        }

        if (draft.EventDate == default)
        {
            errors.Add("Event date is required.");
        }

        if (draft.Importance < MinImportance || draft.Importance > MaxImportance)
        {
            errors.Add($"Importance must be between {MinImportance} and {MaxImportance}.");
        }

        if (!string.IsNullOrWhiteSpace(draft.SourceUrl) && draft.SourceUrl.Length > MaxSourceUrlLength)
        {
            errors.Add($"Source URL must be {MaxSourceUrlLength} characters or fewer.");
        }
        else if (!string.IsNullOrWhiteSpace(draft.SourceUrl) && !NewsValidation.IsSafePublicUrl(draft.SourceUrl))
        {
            errors.Add("Source URL must be an absolute http or https URL.");
        }

        return errors;
    }
}
