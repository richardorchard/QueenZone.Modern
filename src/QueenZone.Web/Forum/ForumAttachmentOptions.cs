namespace QueenZone.Web;

public sealed class ForumAttachmentOptions
{
    public const string SectionName = "ForumAttachments";

    public int MaxFilesPerPost { get; set; } = 5;

    public long MaxBytesPerFile { get; set; } = 20 * 1024 * 1024;

    public long MaxTotalBytesPerPost { get; set; } = 50 * 1024 * 1024;

    public List<string> AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "application/pdf",
        "application/zip",
        "application/x-zip-compressed",
        "audio/mpeg",
        "audio/mp3",
        "audio/flac",
        "audio/x-flac",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    ];
}
