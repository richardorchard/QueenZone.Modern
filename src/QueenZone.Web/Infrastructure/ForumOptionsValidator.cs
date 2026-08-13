using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

public sealed class ForumOptionsValidator : IValidateOptions<ForumOptions>
{
    /// <summary>Maximum finite member edit window (365 days).</summary>
    public const int MaxPostEditWindowMinutes = 60 * 24 * 365;

    public ValidateOptionsResult Validate(string? name, ForumOptions options)
    {
        if (options.PostEditWindowMinutes == -1)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.PostEditWindowMinutes < 0 || options.PostEditWindowMinutes > MaxPostEditWindowMinutes)
        {
            return ValidateOptionsResult.Fail(
                $"{ForumOptions.SectionName}:PostEditWindowMinutes must be -1 (unlimited), 0 (disabled), " +
                $"or between 1 and {MaxPostEditWindowMinutes}.");
        }

        return ValidateOptionsResult.Success;
    }
}
