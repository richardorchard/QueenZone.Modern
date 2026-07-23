using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace QueenZone.Storage;

/// <summary>
/// Shared WebP image derivatives for gallery and UGC photo pipelines.
/// WebP is the default public derivative format going forward.
/// </summary>
public static class PhotoWebpDerivatives
{
    public const int DefaultThumbSizePixels = 400;

    public const int DefaultWebMaxLongestSide = 2000;

    public const int DefaultWebpQuality = 85;

    public const string WebpContentType = "image/webp";

    public const string WebpExtension = ".webp";

    public sealed class EncodedWebp : IAsyncDisposable
    {
        public EncodedWebp(MemoryStream stream, int widthPx, int heightPx)
        {
            Stream = stream;
            WidthPx = widthPx;
            HeightPx = heightPx;
        }

        public MemoryStream Stream { get; }

        public int WidthPx { get; }

        public int HeightPx { get; }

        public ValueTask DisposeAsync()
        {
            Stream.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Creates a center-cropped square WebP thumbnail (default 400×400).
    /// </summary>
    public static async Task<EncodedWebp> CreateSquareThumbnailAsync(
        Image image,
        int sizePixels = DefaultThumbSizePixels,
        int quality = DefaultWebpQuality,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (sizePixels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sizePixels));
        }

        using var clone = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(sizePixels, sizePixels),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            });
        });

        var output = new MemoryStream();
        await clone.SaveAsync(output, new WebpEncoder { Quality = quality }, cancellationToken);
        output.Position = 0;
        return new EncodedWebp(output, clone.Width, clone.Height);
    }

    /// <summary>
    /// Creates a WebP derivative capped on the longest side (default 2000 px).
    /// </summary>
    public static async Task<EncodedWebp> CreateMaxSideAsync(
        Image image,
        int maxLongestSide = DefaultWebMaxLongestSide,
        int quality = DefaultWebpQuality,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (maxLongestSide < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLongestSide));
        }

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
        await clone.SaveAsync(output, new WebpEncoder { Quality = quality }, cancellationToken);
        output.Position = 0;
        return new EncodedWebp(output, clone.Width, clone.Height);
    }

    /// <summary>
    /// Builds a legacy-style thumb blob leaf name with a WebP extension,
    /// e.g. <c>celeb2.jpg</c> → <c>celeb2_t.webp</c>.
    /// </summary>
    public static string ToThumbnailBlobName(string originalBlobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalBlobName);
        var leaf = originalBlobName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        var stem = Path.GetFileNameWithoutExtension(leaf);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "thumb";
        }

        return $"{stem}_t{WebpExtension}";
    }

    /// <summary>
    /// Builds a <c>PIC_FILES_T</c>-style legacy path for a thumb in the same folder as the original.
    /// </summary>
    public static string ToLegacyThumbnailPath(string originalLegacyPath, string thumbnailBlobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalLegacyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbnailBlobName);

        var trimmed = originalLegacyPath.Trim().TrimEnd('/');
        var slash = trimmed.LastIndexOf('/');
        if (slash <= 0)
        {
            return "/" + thumbnailBlobName.TrimStart('/');
        }

        return trimmed[..(slash + 1)] + thumbnailBlobName;
    }
}
