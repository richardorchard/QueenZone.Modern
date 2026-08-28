using QueenZone.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace QueenZone.Web;

/// <summary>
/// Validates article card-image uploads and produces 3:2 WebP full + thumbnail
/// derivatives. The crop ratio matches <c>.qz-article-card__media</c>
/// (<c>aspect-ratio: 3 / 2</c> in site.css).
/// </summary>
public static class NewsArticleImageProcessor
{
    public const int CardAspectWidth = 3;

    public const int CardAspectHeight = 2;

    public const int MinCropWidth = 400;

    public const int MinCropHeight = 267;

    public const long MaxUploadBytes = 10 * 1024 * 1024;

    public const double CropAspectTolerance = 0.05;

    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

    public sealed record ProcessedArticleImage(MemoryStream FullImage, MemoryStream Thumbnail);

    public static async Task<ProcessedArticleImage> ProcessAsync(
        Stream source,
        string originalFileName,
        NewsArticleImageCrop? crop = null,
        long maxBytes = MaxUploadBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (maxBytes <= 0)
        {
            maxBytes = MaxUploadBytes;
        }

        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0)
        {
            throw new InvalidOperationException("An article image is required.");
        }

        if (buffer.Length > maxBytes)
        {
            throw new InvalidOperationException(
                $"Article image must be {maxBytes} bytes or smaller.");
        }

        buffer.Position = 0;
        var headerLength = (int)Math.Min(64, buffer.Length);
        var header = new byte[headerLength];
        var read = await buffer.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        buffer.Position = 0;

        var sniffed = BlobContentSniffer.TryDetectContentType(header.AsSpan(0, read));
        if (sniffed is null || !AllowedContentTypes.Contains(NormalizeJpeg(sniffed)))
        {
            throw new InvalidOperationException("Article image must be a JPEG, PNG, or WebP file.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var fromExt = BlobContentSniffer.GuessContentTypeFromExtension(extension);
            if (fromExt is not null)
            {
                var normalizedExt = NormalizeJpeg(fromExt);
                if (!AllowedContentTypes.Contains(normalizedExt))
                {
                    throw new InvalidOperationException("Article image must be a JPEG, PNG, or WebP file.");
                }

                if (!string.Equals(normalizedExt, NormalizeJpeg(sniffed), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("File extension does not match the image content.");
                }
            }
        }

        try
        {
            using var image = await Image.LoadAsync(buffer, cancellationToken);
            var rect = ResolveCrop(image.Width, image.Height, crop);
            if (rect.Width < MinCropWidth || rect.Height < MinCropHeight)
            {
                throw new InvalidOperationException(
                    $"Article image is too small. Use at least {MinCropWidth}×{MinCropHeight} pixels.");
            }

            using var cropped = image.Clone(ctx => ctx.Crop(rect));
            await using var fullEncoded = await PhotoWebpDerivatives.CreateMaxSideAsync(
                cropped,
                UgcProxyPaths.FullMaxLongestSide,
                cancellationToken: cancellationToken);
            await using var thumbEncoded = await PhotoWebpDerivatives.CreateMaxSideAsync(
                cropped,
                UgcProxyPaths.ThumbMaxLongestSide,
                cancellationToken: cancellationToken);

            var full = CopyToMemoryStream(fullEncoded.Stream);
            var thumb = CopyToMemoryStream(thumbEncoded.Stream);
            return new ProcessedArticleImage(full, thumb);
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException("Article image must be a JPEG, PNG, or WebP file.");
        }
        catch (InvalidImageContentException)
        {
            throw new InvalidOperationException("Article image could not be read.");
        }
    }

    internal static Rectangle ResolveCrop(int width, int height, NewsArticleImageCrop? requested)
    {
        var fallback = CenterCardCrop(width, height);
        if (requested is null)
        {
            return fallback;
        }

        var crop = requested.Value;
        if (crop.X < 0
            || crop.Y < 0
            || crop.Width < 1
            || crop.Height < 1
            || crop.X + crop.Width > width
            || crop.Y + crop.Height > height)
        {
            return fallback;
        }

        var aspect = crop.Width / (double)crop.Height;
        var expected = CardAspectWidth / (double)CardAspectHeight;
        if (Math.Abs(aspect - expected) / expected > CropAspectTolerance)
        {
            return fallback;
        }

        if (crop.Width < MinCropWidth || crop.Height < MinCropHeight)
        {
            throw new InvalidOperationException(
                $"The selected crop is too small. Use at least {MinCropWidth}×{MinCropHeight} pixels.");
        }

        return new Rectangle(crop.X, crop.Y, crop.Width, crop.Height);
    }

    internal static Rectangle CenterCardCrop(int width, int height)
    {
        if (width < 1 || height < 1)
        {
            return new Rectangle(0, 0, Math.Max(width, 0), Math.Max(height, 0));
        }

        var targetAspect = CardAspectWidth / (double)CardAspectHeight;
        var imageAspect = width / (double)height;
        if (imageAspect > targetAspect)
        {
            var cropWidth = Math.Max(1, (int)Math.Round(height * targetAspect));
            cropWidth = Math.Min(cropWidth, width);
            var x = (width - cropWidth) / 2;
            return new Rectangle(x, 0, cropWidth, height);
        }

        var cropHeight = Math.Max(1, (int)Math.Round(width / targetAspect));
        cropHeight = Math.Min(cropHeight, height);
        var y = (height - cropHeight) / 2;
        return new Rectangle(0, y, width, cropHeight);
    }

    private static string NormalizeJpeg(string contentType) =>
        string.Equals(contentType, "image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : contentType;

    private static MemoryStream CopyToMemoryStream(Stream source)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var copy = new MemoryStream();
        source.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }
}

public readonly record struct NewsArticleImageCrop(int X, int Y, int Width, int Height);
