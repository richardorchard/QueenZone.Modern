using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class NewsForumOptionsValidator : IValidateOptions<NewsForumOptions>
{
    public ValidateOptionsResult Validate(string? name, NewsForumOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SystemMemberEmail)
            || !options.SystemMemberEmail.Contains('@', StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                $"{NewsForumOptions.SectionName}:SystemMemberEmail must be a non-empty email.");
        }

        if (string.IsNullOrWhiteSpace(options.SystemMemberDisplayName))
        {
            return ValidateOptionsResult.Fail(
                $"{NewsForumOptions.SectionName}:SystemMemberDisplayName is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
