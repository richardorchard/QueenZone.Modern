namespace QueenZone.Data;

public static class TriviaValidation
{
    public const int MaxTextLength = 1000;

    public const int MaxCategoryLength = 100;

    public const int MaxDifficultyLength = 20;

    public const int MaxSourceLength = 500;

    public static readonly IReadOnlyList<string> AllowedDifficulties = ["easy", "medium", "hard"];

    public static IReadOnlyList<string> ValidateDraft(AdminTriviaDraft draft)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(draft.Text))
        {
            errors.Add("Fact text is required.");
        }
        else if (draft.Text.Length > MaxTextLength)
        {
            errors.Add($"Fact text must be {MaxTextLength} characters or fewer.");
        }

        if (draft.Category is { Length: > MaxCategoryLength })
        {
            errors.Add($"Category must be {MaxCategoryLength} characters or fewer.");
        }

        if (draft.Difficulty is not null &&
            !AllowedDifficulties.Contains(draft.Difficulty, StringComparer.Ordinal))
        {
            errors.Add("Difficulty must be easy, medium, or hard.");
        }

        if (draft.Source is { Length: > MaxSourceLength })
        {
            errors.Add($"Source must be {MaxSourceLength} characters or fewer.");
        }

        return errors;
    }
}
