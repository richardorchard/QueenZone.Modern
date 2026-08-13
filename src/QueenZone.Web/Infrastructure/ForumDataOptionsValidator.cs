using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Startup check for <see cref="ForumDataOptions"/>. <c>UseModernForumReads = false</c> remains a
/// documented emergency rollback and is allowed in every environment.
/// </summary>
public sealed class ForumDataOptionsValidator : IValidateOptions<ForumDataOptions>
{
    public ValidateOptionsResult Validate(string? name, ForumDataOptions options)
    {
        // Bool bind is the only setting today. ValidateOnStart still forces the section to load
        // at boot so a malformed App Service override fails closed instead of defaulting silently.
        _ = options.UseModernForumReads;
        return ValidateOptionsResult.Success;
    }
}
