namespace QueenZone.Data;

public static class QuoteValidation
{
    public const int MaxTextLength = 1000;

    public const int MaxWhoSaidLength = 50;

    public const int MaxContextLength = 500;

    public static IReadOnlyList<string> ValidateDraft(AdminQuoteDraft draft)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(draft.Text))
        {
            errors.Add("Quote text is required.");
        }
        else if (draft.Text.Length > MaxTextLength)
        {
            errors.Add($"Quote text must be {MaxTextLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(draft.WhoSaid))
        {
            errors.Add("Who said it is required.");
        }
        else if (draft.WhoSaid.Length > MaxWhoSaidLength)
        {
            errors.Add($"Who said it must be {MaxWhoSaidLength} characters or fewer.");
        }

        if (draft.Context is { Length: > MaxContextLength })
        {
            errors.Add($"Context must be {MaxContextLength} characters or fewer.");
        }

        return errors;
    }
}
