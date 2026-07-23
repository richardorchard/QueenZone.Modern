using SixLabors.ImageSharp;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Validates member photo submissions and produces original + web + thumbnail payloads.
/// Public derivatives default to WebP via <see cref="PhotoWebpDerivatives"/>.
/// </summary>
public static class PhotoSubmissionImageProcessor
{
    public const long MaxUploadBytes = 20 * 1024 * 1024;

    public const int WebMaxLongestSide = PhotoWebpDerivatives.DefaultWebMaxLongestSide;

    public const int ThumbSizePixels = PhotoWebpDerivatives.DefaultThumbSizePixels;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/tiff",
    };

    public sealed record ProcessedPhotoSubmission(
        MemoryStream Original,
        MemoryStream WebOptimized,
        MemoryStream Thumbnail,
        string MimeType,
        int WidthPx,
        int HeightPx,
        long OriginalSizeBytes);

    /// <summary>
    /// Validates size and MIME type, keeps the original bytes, and generates WebP derivatives.
    /// Throws <see cref="InvalidOperationException"/> for expected client errors.
    /// </summary>
    public static async Task<ProcessedPhotoSubmission> ProcessAsync(
        Stream source,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0)
        {
            throw new InvalidOperationException("A photo file is required.");
        }

        if (buffer.Length > MaxUploadBytes)
        {
            throw new InvalidOperationException(
                $"Photo must be {MaxUploadBytes / (1024 * 1024)} MB or smaller.");
        }

        buffer.Position = 0;
        var headerLength = (int)Math.Min(64, buffer.Length);
        var header = new byte[headerLength];
        var read = await buffer.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        buffer.Position = 0;

        var sniffed = BlobContentSniffer.TryDetectContentType(header.AsSpan(0, read));
        if (sniffed is null || !AllowedContentTypes.Contains(sniffed))
        {
            throw new InvalidOperationException("Photo must be a JPEG, PNG, WebP, or TIFF image.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var fromExt = BlobContentSniffer.GuessContentTypeFromExtension(extension);
            if (fromExt is not null
                && !fromExt.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("File extension does not match the image content.");
            }

            if (fromExt is not null
                && !string.Equals(fromExt, sniffed, StringComparison.OrdinalIgnoreCase)
                && !(IsJpegFamily(fromExt) && IsJpegFamily(sniffed)))
            {
                throw new InvalidOperationException("File extension does not match the image content.");
            }
        }

        try
        {
            using var image = await Image.LoadAsync(buffer, cancellationToken);
            var width = image.Width;
            var height = image.Height;

            var web = await PhotoWebpDerivatives.CreateMaxSideAsync(
                image,
                WebMaxLongestSide,
                cancellationToken: cancellationToken);
            var thumb = await PhotoWebpDerivatives.CreateSquareThumbnailAsync(
                image,
                ThumbSizePixels,
                cancellationToken: cancellationToken);

            var original = new MemoryStream();
            buffer.Position = 0;
            await buffer.CopyToAsync(original, cancellationToken);
            original.Position = 0;

            return new ProcessedPhotoSubmission(
                original,
                web.Stream,
                thumb.Stream,
                sniffed,
                width,
                height,
                original.Length);
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException("Photo must be a JPEG, PNG, WebP, or TIFF image.");
        }
        catch (InvalidImageContentException)
        {
            throw new InvalidOperationException("Photo image could not be read.");
        }
    }

    private static bool IsJpegFamily(string contentType) =>
        string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)
        || string.Equals(contentType, "image/jpg", StringComparison.OrdinalIgnoreCase);
}
