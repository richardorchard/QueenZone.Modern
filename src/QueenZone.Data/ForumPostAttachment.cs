namespace QueenZone.Data;

public sealed record ForumPostAttachment(string FileName, long? FileSizeBytes)
{
    public string Url => $"https://cdn.queenzone.org/attachments/{FileName}";

    public string Extension => Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();

    public string FormattedSize => FileSizeBytes switch
    {
        null or 0 => string.Empty,
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };

    public static IReadOnlyList<ForumPostAttachment>? Parse(string? attachment, string? filesize)
    {
        if (string.IsNullOrWhiteSpace(attachment))
        {
            return null;
        }

        long? bytes = long.TryParse(filesize?.Trim(), out var parsed) ? parsed : null;
        return [new ForumPostAttachment(attachment.Trim(), bytes)];
    }
}
