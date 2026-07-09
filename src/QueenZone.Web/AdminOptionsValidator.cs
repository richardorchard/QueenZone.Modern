using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class AdminOptionsValidator : IValidateOptions<AdminOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminOptions options)
    {
        if (options.AllowedEmails is null || options.AllowedEmails.Length == 0)
        {
            return ValidateOptionsResult.Fail(
                $"{AdminOptions.SectionName}:AllowedEmails must contain at least one admin email.");
        }

        if (options.AllowedEmails.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail(
                $"{AdminOptions.SectionName}:AllowedEmails must not contain blank entries.");
        }

        return ValidateOptionsResult.Success;
    }
}
