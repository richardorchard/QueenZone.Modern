using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class ForumAttachmentValidator(IOptions<ForumAttachmentOptions> options)
{
    private readonly ForumAttachmentOptions options = options.Value;

    public ForumAttachmentValidationResult Validate(IReadOnlyList<IFormFile> files)
    {
        var errors = new List<string>();
        var accepted = new List<IFormFile>();

        if (files.Count == 0)
        {
            return ForumAttachmentValidationResult.Empty;
        }

        if (files.Count > options.MaxFilesPerPost)
        {
            errors.Add($"You can attach at most {options.MaxFilesPerPost} files per post.");
            return new ForumAttachmentValidationResult(accepted, errors);
        }

        long totalBytes = 0;
        var allowed = new HashSet<string>(options.AllowedContentTypes, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file is null || file.Length <= 0)
            {
                errors.Add("One of the selected files was empty.");
                continue;
            }

            var name = string.IsNullOrWhiteSpace(file.FileName) ? "file" : Path.GetFileName(file.FileName);

            if (file.Length > options.MaxBytesPerFile)
            {
                errors.Add(
                    $"'{name}' is too large ({FormatBytes(file.Length)}). Max per file is {FormatBytes(options.MaxBytesPerFile)}.");
                continue;
            }

            totalBytes += file.Length;
            if (totalBytes > options.MaxTotalBytesPerPost)
            {
                errors.Add(
                    $"Attachments total {FormatBytes(totalBytes)}, which exceeds the {FormatBytes(options.MaxTotalBytesPerPost)} limit per post.");
                // Do not accept further files once the total is exceeded.
                break;
            }

            var headerType = string.IsNullOrWhiteSpace(file.ContentType)
                ? string.Empty
                : file.ContentType.Trim();
            var guessedType = GuessContentType(name);
            var allowedByHeader = !string.IsNullOrEmpty(headerType) && allowed.Contains(headerType);
            var allowedByExtension = allowed.Contains(guessedType);
            if (!allowedByHeader && !allowedByExtension)
            {
                var shown = !string.IsNullOrEmpty(headerType) ? headerType : guessedType;
                errors.Add($"'{name}' has a type that is not allowed ({shown}).");
                continue;
            }

            accepted.Add(file);
        }

        return new ForumAttachmentValidationResult(accepted, errors);
    }

    public static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };

    public static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".mp3" => "audio/mpeg",
            ".flac" => "audio/flac",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream",
        };
    }
}

public sealed record ForumAttachmentValidationResult(
    IReadOnlyList<IFormFile> AcceptedFiles,
    IReadOnlyList<string> Errors)
{
    public static ForumAttachmentValidationResult Empty { get; } = new([], []);

    public bool IsValid => Errors.Count == 0;
}
