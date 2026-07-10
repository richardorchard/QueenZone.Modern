using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Validates editor image uploads and produces full + thumbnail WebP payloads
/// (max longest side from <see cref="UgcProxyPaths"/>).
/// </summary>
public static class EditorImageProcessor
{
    public sealed record ProcessedEditorImage(
        MemoryStream FullImage,
        MemoryStream Thumbnail,
        string FullFileName,
        string ThumbFileName);

    public static async Task<ProcessedEditorImage> ProcessAsync(
        Stream source,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0)
        {
            throw new InvalidOperationException("An image file is required.");
        }

        if (buffer.Length > EditorImageUploadEndpoints.MaxImageBytes)
        {
            throw new InvalidOperationException(
                $"Image must be {EditorImageUploadEndpoints.MaxImageBytes} bytes or smaller.");
        }

        buffer.Position = 0;
        var headerLength = (int)Math.Min(64, buffer.Length);
        var header = new byte[headerLength];
        var read = await buffer.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        buffer.Position = 0;

        var sniffed = BlobContentSniffer.TryDetectContentType(header.AsSpan(0, read));
        if (sniffed is null || !sniffed.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only image uploads are allowed.");
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
            var full = await EncodeMaxSideAsync(image, UgcProxyPaths.FullMaxLongestSide, cancellationToken);
            var thumb = await EncodeMaxSideAsync(image, UgcProxyPaths.ThumbMaxLongestSide, cancellationToken);

            var baseName = Path.GetFileNameWithoutExtension(
                string.IsNullOrWhiteSpace(originalFileName) ? "paste" : originalFileName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "paste";
            }

            // Storage uses generated Guid names; these are only used when PreferredBlobName is not set.
            var fullFileName = baseName + ".webp";
            var thumbFileName = baseName + "-thumb.webp";

            return new ProcessedEditorImage(full, thumb, fullFileName, thumbFileName);
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException("Only image uploads are allowed.");
        }
        catch (InvalidImageContentException)
        {
            throw new InvalidOperationException("Image could not be read.");
        }
    }

    private static bool IsJpegFamily(string contentType) =>
        string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)
        || string.Equals(contentType, "image/jpg", StringComparison.OrdinalIgnoreCase);

    private static async Task<MemoryStream> EncodeMaxSideAsync(
        Image image,
        int maxLongestSide,
        CancellationToken cancellationToken)
    {
        using var clone = image.Clone(ctx =>
        {
            if (image.Width > maxLongestSide || image.Height > maxLongestSide)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxLongestSide, maxLongestSide),
                    Mode = ResizeMode.Max,
                });
            }
        });

        var output = new MemoryStream();
        await clone.SaveAsync(output, new WebpEncoder { Quality = 85 }, cancellationToken);
        output.Position = 0;
        return output;
    }
}
