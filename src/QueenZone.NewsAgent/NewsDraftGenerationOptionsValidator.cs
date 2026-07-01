using Microsoft.Extensions.Options;

namespace QueenZone.NewsAgent;

public sealed class NewsDraftGenerationOptionsValidator : IValidateOptions<NewsDraftGenerationOptions>
{
    public ValidateOptionsResult Validate(string? name, NewsDraftGenerationOptions options)
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
