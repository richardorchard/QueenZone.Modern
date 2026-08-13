using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class ForumAttachmentOptionsValidator : IValidateOptions<ForumAttachmentOptions>
{
    public const int MaxFilesPerPostCeiling = 50;

    public const long MaxBytesPerFileCeiling = 100L * 1024 * 1024;

    public const long MaxTotalBytesPerPostCeiling = 500L * 1024 * 1024;

    public ValidateOptionsResult Validate(string? name, ForumAttachmentOptions options)
    {
        var failures = new List<string>();
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{ForumAttachmentOptions.SectionName}:MaxFilesPerPost",
            options.MaxFilesPerPost,
            MaxFilesPerPostCeiling);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{ForumAttachmentOptions.SectionName}:MaxBytesPerFile",
            options.MaxBytesPerFile,
            MaxBytesPerFileCeiling);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{ForumAttachmentOptions.SectionName}:MaxTotalBytesPerPost",
            options.MaxTotalBytesPerPost,
            MaxTotalBytesPerPostCeiling);

        if (options.MaxBytesPerFile > 0
            && options.MaxTotalBytesPerPost > 0
            && options.MaxTotalBytesPerPost < options.MaxBytesPerFile)
        {
            failures.Add(
                $"{ForumAttachmentOptions.SectionName}:MaxTotalBytesPerPost must be greater than or equal to MaxBytesPerFile.");
        }

        OptionsValidation.RequireNonBlankEntries(
            failures,
            $"{ForumAttachmentOptions.SectionName}:AllowedContentTypes",
            options.AllowedContentTypes,
            requireAtLeastOne: true);

        return OptionsValidation.Result(failures);
    }
}
