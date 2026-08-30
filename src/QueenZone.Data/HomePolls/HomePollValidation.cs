namespace QueenZone.Data;

public static class HomePollValidation
{
    public const int MinOptions = 2;

    public const int MaxOptions = 10;

    public const int QuestionMaxLength = 300;

    public const int OptionMaxLength = 200;

    public static IReadOnlyList<string> NormalizeOptions(IEnumerable<string?> options) =>
        (options ?? [])
            .Select(option => option?.Trim() ?? string.Empty)
            .Where(option => option.Length > 0)
            .ToList();

    public static IReadOnlyList<string> ValidateDraft(AdminHomePollDraft draft)
    {
        var errors = new List<string>();
        var question = draft.Question?.Trim() ?? string.Empty;
        if (question.Length == 0)
        {
            errors.Add("Question is required.");
        }
        else if (question.Length > QuestionMaxLength)
        {
            errors.Add($"Question must be {QuestionMaxLength} characters or fewer.");
        }

        var options = NormalizeOptions(draft.Options);
        if (options.Count is < MinOptions or > MaxOptions)
        {
            errors.Add($"Polls require between {MinOptions} and {MaxOptions} options.");
        }

        if (options.Any(option => option.Length > OptionMaxLength))
        {
            errors.Add($"Each option must be {OptionMaxLength} characters or fewer.");
        }

        return errors;
    }
}
