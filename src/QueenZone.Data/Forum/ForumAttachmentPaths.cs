namespace QueenZone.Data;

/// <summary>
/// App-relative download paths and legacy CDN redirect targets for forum attachments.
/// HTML never links straight to public blob hosts for downloads; members hit the app first.
/// </summary>
public static class ForumAttachmentPaths
{
    public const string DefaultLegacyAttachmentsBaseUrl = "https://cdn2.queenzone.org/attachments";

    public const string LegacyAttachmentsBaseUrlEnvironmentVariable = "QUEENZONE_FORUM_ATTACHMENTS_BASE_URL";

    public static string LegacyDownloadPath(int legacyPostId) =>
        $"/forum/attachment/legacy/{legacyPostId}";

    public static string DownloadPath(int legacyPostId, Guid attachmentId) =>
        $"/forum/attachment/{legacyPostId}/{attachmentId:D}";

    /// <summary>
    /// Cloudflare Worker proxy for the legacy <c>attachments</c> blob container.
    /// Used after member auth so Content-Disposition can be set at the edge when needed.
    /// </summary>
    public static string BuildLegacyCdnUrl(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var baseUrl = Environment.GetEnvironmentVariable(LegacyAttachmentsBaseUrlEnvironmentVariable)
            ?? DefaultLegacyAttachmentsBaseUrl;
        return $"{baseUrl.TrimEnd('/')}/{fileName.Trim().TrimStart('/')}";
    }
}
