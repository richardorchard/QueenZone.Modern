using Microsoft.Extensions.Options;

namespace QueenZone.NewsAgent;

public sealed class NewsTriageOptionsValidator : IValidateOptions<NewsTriageOptions>
{
    public ValidateOptionsResult Validate(string? name, NewsTriageOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
