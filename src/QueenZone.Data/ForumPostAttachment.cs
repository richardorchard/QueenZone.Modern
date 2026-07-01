namespace QueenZone.Data;

public sealed record ForumPostAttachment(string FileName, long? FileSizeBytes)
{
    public string Url => $"https://pictures.queenzone.org/attachments/{FileName}";

    public string Extension => Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();

    public string FormattedSize => FileSizeBytes switch
    {
        null or 0 => string.Empty,
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
